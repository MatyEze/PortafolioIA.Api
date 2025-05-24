using Application.Interfaces;
using Domain.Entities;
using Domain.ValueObjects;
using FastEndpoints;
using Mapster;
using System.Diagnostics;

namespace Api.Endpoints.ProcessFile;

public class ProcessFileEndpoint : Endpoint<ProcessFileRequest, ProcessFileResponse>
{
    private readonly IDataPointRepository _dataPointRepository;
    private readonly IFileParsingService _fileParsingService;

    public ProcessFileEndpoint(
        IDataPointRepository dataPointRepository,
        IFileParsingService fileParsingService)
    {
        _dataPointRepository = dataPointRepository;
        _fileParsingService = fileParsingService;
    }

    public override void Configure()
    {
        Post("/api/portfolio/process-file");
        AllowFileUploads();
        AllowAnonymous(); // Ajustar según necesidades de auth

        Summary(s =>
        {
            s.Summary = "Procesar archivo de movimientos de broker";
            s.Description = "Recibe un archivo de un broker específico y lo procesa para generar movimientos financieros";
            s.RequestParam(r => r.File, "Archivo Excel/CSV con el historial de movimientos");
            s.RequestParam(r => r.BrokerKey, "Clave del broker (IOL, BALANZ, BULL)");
            s.Responses[200] = "Archivo procesado exitosamente";
            s.Responses[400] = "Archivo o parámetros inválidos";
            s.Responses[409] = "Archivo ya procesado anteriormente";
            s.Responses[500] = "Error interno del servidor";
        });
    }

    public override async Task HandleAsync(ProcessFileRequest request, CancellationToken ct)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            // 1. Verificar que el parser puede manejar el archivo
            if (!_fileParsingService.CanParse(request.BrokerKey, request.File.FileName))
            {
                var supportedBrokers = string.Join(", ", _fileParsingService.GetSupportedBrokers());
                var supportedExtensions = string.Join(", ", _fileParsingService.GetSupportedExtensions());

                ThrowError($"No se puede procesar el archivo '{request.File.FileName}' para el broker '{request.BrokerKey}'");
                ThrowError($"Brokers soportados: {supportedBrokers}");
                ThrowError($"Extensiones soportadas: {supportedExtensions}");
            }

            // 2. Verificar si el archivo ya fue procesado
            var fileExists = await _dataPointRepository.ExistsWithSameFileAsync(
                request.File.FileName,
                request.File.Length);

            if (fileExists)
            {
                await SendAsync(new ProcessFileResponse
                {
                    DataPointId = Guid.Empty,
                    Status = "Failed",
                    FileName = request.File.FileName,
                    BrokerKey = request.BrokerKey,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ProcessedAt = DateTime.UtcNow,
                    Errores = new List<string>
                    {
                        "Este archivo ya ha sido procesado anteriormente",
                        "Verifique el historial de archivos procesados"
                    }
                }, 409, ct);
                return;
            }

            // 3. Crear FileMetadata
            var fileMetadata = new FileMetadata(
                request.File.FileName,
                request.File.Length,
                request.File.ContentType ?? "application/octet-stream");

            // 4. Crear y guardar DataPoint
            var dataPoint = DataPoint.Create(fileMetadata);
            await _dataPointRepository.AddAsync(dataPoint);

            // 5. Iniciar procesamiento
            dataPoint.StartProcessing();
            await _dataPointRepository.UpdateAsync(dataPoint);

            // 6. Parsear archivo
            using var stream = request.File.OpenReadStream();
            var parsingResult = await _fileParsingService.ParseFileAsync(
                stream,
                request.File.FileName,
                request.BrokerKey,
                dataPoint.Id);

            // 7. Verificar si el parsing fue exitoso
            if (!parsingResult.IsSuccess)
            {
                dataPoint.MarkFailed(string.Join("; ", parsingResult.Errores));
                await _dataPointRepository.UpdateAsync(dataPoint);

                stopwatch.Stop();

                var failureResponse = new ProcessFileResponse
                {
                    DataPointId = dataPoint.Id,
                    Status = "Failed",
                    FileName = request.File.FileName,
                    BrokerKey = request.BrokerKey,
                    MovimientosCount = 0,
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    ProcessedAt = DateTime.UtcNow,
                    Errores = parsingResult.Errores,
                    Advertencias = parsingResult.Advertencias
                };

                await SendOkAsync(failureResponse, ct);
                return;
            }

            // 8. Agregar movimientos al DataPoint
            dataPoint.AddMovements(parsingResult.Movimientos);
            dataPoint.MarkCompleted();

            // 9. Guardar cambios finales
            await _dataPointRepository.UpdateAsync(dataPoint);

            stopwatch.Stop();

            // 10. Crear respuesta exitosa usando Mapster
            var response = new ProcessFileResponse
            {
                DataPointId = dataPoint.Id,
                Status = dataPoint.Status.ToString(),
                FileName = request.File.FileName,
                BrokerKey = request.BrokerKey,
                MovimientosCount = parsingResult.Movimientos.Count,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ProcessedAt = DateTime.UtcNow,
                Errores = new List<string>(),
                Advertencias = parsingResult.Advertencias,
                Summary = CreateSummary(parsingResult.Movimientos)
            };

            await SendOkAsync(response, ct);
        }
        catch (InvalidOperationException ex)
        {
            // Errores de negocio/dominio
            stopwatch.Stop();

            await SendAsync(new ProcessFileResponse
            {
                DataPointId = Guid.Empty,
                Status = "Failed",
                FileName = request.File.FileName,
                BrokerKey = request.BrokerKey,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ProcessedAt = DateTime.UtcNow,
                Errores = new List<string> { ex.Message }
            }, 400, ct);
        }
        catch (Exception ex)
        {
            // Errores inesperados
            stopwatch.Stop();

            var errorResponse = new ProcessFileResponse
            {
                DataPointId = Guid.Empty,
                Status = "Failed",
                FileName = request.File.FileName,
                BrokerKey = request.BrokerKey,
                MovimientosCount = 0,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                ProcessedAt = DateTime.UtcNow,
                Errores = new List<string> { "Error interno del servidor", ex.Message }
            };

            await SendAsync(errorResponse, 500, ct);
        }
    }

    private static ProcessingSummaryDto CreateSummary(List<Domain.Entities.Movimiento> movimientos)
    {
        if (!movimientos.Any())
        {
            return new ProcessingSummaryDto();
        }

        var summary = new ProcessingSummaryDto
        {
            TotalCompras = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Compra),
            TotalVentas = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Venta),
            TotalDepositos = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Deposito),
            TotalExtracciones = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Extraccion),
            TotalDividendos = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Dividendos),
            TotalCauciones = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Caucion ||
                                                    m.Tipo == Domain.Entities.TipoMovimiento.LiquidacionCaucion),
            TotalOtros = movimientos.Count(m => m.Tipo == Domain.Entities.TipoMovimiento.Otro ||
                                               m.Tipo == Domain.Entities.TipoMovimiento.SuscripcionFondo ||
                                               m.Tipo == Domain.Entities.TipoMovimiento.RescateFondo ||
                                               m.Tipo == Domain.Entities.TipoMovimiento.Credito),
            MontoTotalOperado = movimientos.Sum(m => Math.Abs(m.MontoTotal)),
            MovimientosPorTicker = movimientos
                .Where(m => !string.IsNullOrEmpty(m.Ticker))
                .GroupBy(m => m.Ticker!)
                .ToDictionary(g => g.Key, g => g.Count()),
            MontosPorMoneda = movimientos
                .GroupBy(m => m.Moneda.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(m => Math.Abs(m.MontoTotal))),
            FechaDesde = movimientos.Min(m => m.FechaConcertacion),
            FechaHasta = movimientos.Max(m => m.FechaConcertacion)
        };

        return summary;
    }
}