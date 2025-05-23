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
            s.Description = "Recibe un archivo de un broker específico y lo procesa para generar movimientos";
            s.Responses[200] = "Archivo procesado exitosamente";
            s.Responses[400] = "Archivo o parámetros inválidos";
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
                await SendErrorsAsync(400, ct);
                return;
            }

            // 2. Crear FileMetadata
            var fileMetadata = new FileMetadata(
                request.File.FileName,
                request.File.Length,
                request.File.ContentType);

            // 3. Crear DataPoint
            var dataPoint = DataPoint.Create(fileMetadata);
            await _dataPointRepository.AddAsync(dataPoint);

            // 4. Iniciar procesamiento
            dataPoint.StartProcessing();

            // 5. Parsear archivo
            using var stream = request.File.OpenReadStream();
            var parsingResult = await _fileParsingService.ParseFileAsync(
                stream,
                request.File.FileName,
                request.BrokerKey,
                dataPoint.Id);

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
                    ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                    Errores = parsingResult.Errores,
                    Advertencias = parsingResult.Advertencias
                };

                await SendOkAsync(failureResponse, ct);
                return;
            }

            // 6. Agregar movimientos al DataPoint
            dataPoint.AddMovements(parsingResult.Movimientos);
            dataPoint.MarkCompleted();

            // 7. Guardar cambios
            await _dataPointRepository.UpdateAsync(dataPoint);

            stopwatch.Stop();

            // 8. Crear respuesta exitosa
            var response = dataPoint.Adapt<ProcessFileResponse>();
            response.BrokerKey = request.BrokerKey;
            response.ProcessingTimeMs = stopwatch.ElapsedMilliseconds;
            response.Advertencias = parsingResult.Advertencias;
            response.Summary = parsingResult.Adapt<ProcessingSummaryDto>();

            await SendOkAsync(response, ct);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            var errorResponse = new ProcessFileResponse
            {
                DataPointId = Guid.Empty,
                Status = "Failed",
                FileName = request.File.FileName,
                BrokerKey = request.BrokerKey,
                ProcessingTimeMs = stopwatch.ElapsedMilliseconds,
                Errores = new List<string> { ex.Message }
            };

            await SendAsync(errorResponse, 500, ct);
        }
    }
}