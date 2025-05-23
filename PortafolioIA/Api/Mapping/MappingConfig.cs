using Api.Endpoints.ProcessFile;
using Application.Dto;
using Application.Interfaces;
using Domain.Entities;
using Mapster;

namespace Api.Mapping
{
    public static class MappingConfig
    {
        public static void RegisterMappings()
        {
            // DataPoint -> ProcessFileResponse
            TypeAdapterConfig<DataPoint, ProcessFileResponse>
                .NewConfig()
                .Map(dest => dest.DataPointId, src => src.Id)
                .Map(dest => dest.Status, src => src.Status.ToString())
                .Map(dest => dest.FileName, src => src.File.FileName)
                .Map(dest => dest.ProcessedAt, src => src.CreatedAt.DateTime)
                .Map(dest => dest.MovimientosCount, src => src.Movements.Count)
                .Map(dest => dest.Errores, src => new List<string>())
                .Map(dest => dest.Advertencias, src => new List<string>());

            // ParsingResult -> ProcessingSummaryDto
            TypeAdapterConfig<ParsingResult, ProcessingSummaryDto>
                .NewConfig()
                .Map(dest => dest.TotalCompras, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Compra))
                .Map(dest => dest.TotalVentas, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Venta))
                .Map(dest => dest.TotalDepositos, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Deposito))
                .Map(dest => dest.TotalExtracciones, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Extraccion))
                .Map(dest => dest.TotalDividendos, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Dividendos))
                .Map(dest => dest.TotalCauciones, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Caucion || m.Tipo == TipoMovimiento.LiquidacionCaucion))
                .Map(dest => dest.TotalOtros, src => src.Movimientos.Count(m => m.Tipo == TipoMovimiento.Otro))
                .Map(dest => dest.MontoTotalOperado, src => src.Movimientos.Sum(m => Math.Abs(m.MontoTotal)))
                .Map(dest => dest.FechaDesde, src => src.Movimientos.Any() ? src.Movimientos.Min(m => m.FechaConcertacion) : DateTime.MinValue)
                .Map(dest => dest.FechaHasta, src => src.Movimientos.Any() ? src.Movimientos.Max(m => m.FechaConcertacion) : DateTime.MinValue);

            // Activo -> ActivoPositionDto
            TypeAdapterConfig<Activo, ActivoPositionDto>
                .NewConfig()
                .Map(dest => dest.ValorTotal, src => src.ObtenerValorTotal());

            // Divisa -> DivisaPositionDto
            TypeAdapterConfig<Divisa, DivisaPositionDto>
                .NewConfig();

            // DataPointStatistics no necesita mapping especial ya que las properties coinciden

            // Lista de Movimientos -> PortfolioMetricsDto (custom mapping)
            TypeAdapterConfig<List<Movimiento>, PortfolioMetricsDto>
                .NewConfig()
                .Map(dest => dest.TotalMovimientos, src => src.Count)
                .Map(dest => dest.TotalComisionesImpuestos, src => src.Sum(m => m.ObtenerTotalComisionesImpuestos()))
                .Map(dest => dest.FechaMovimientoMasAntiguo, src => src.Any() ? src.Min(m => m.FechaConcertacion) : DateTime.MinValue)
                .Map(dest => dest.FechaMovimientoMasReciente, src => src.Any() ? src.Max(m => m.FechaConcertacion) : DateTime.MinValue);

            // Configurar mapping de Enums a string si es necesario
            TypeAdapterConfig<DataPointStatus, string>
                .NewConfig()
                .MapWith(src => src.ToString());

            TypeAdapterConfig<TipoActivo, string>
                .NewConfig()
                .MapWith(src => src.ToString());

            TypeAdapterConfig<TipoMoneda, string>
                .NewConfig()
                .MapWith(src => src.ToString());

            TypeAdapterConfig<TipoMovimiento, string>
                .NewConfig()
                .MapWith(src => src.ToString());
        }

        // Métodos helper para mappings complejos
        public static Dictionary<string, int> MapMovimientosPorTicker(List<Movimiento> movimientos)
        {
            return movimientos
                .Where(m => !string.IsNullOrEmpty(m.Ticker))
                .GroupBy(m => m.Ticker!)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static Dictionary<string, decimal> MapMontosPorMoneda(List<Movimiento> movimientos)
        {
            return movimientos
                .GroupBy(m => m.Moneda.ToString())
                .ToDictionary(g => g.Key, g => g.Sum(m => Math.Abs(m.MontoTotal)));
        }

        public static Dictionary<TipoActivo, int> MapActivosPorTipo(List<Activo> activos)
        {
            return activos
                .GroupBy(a => a.Tipo)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        public static Dictionary<TipoMovimiento, int> MapMovimientosPorTipo(List<Movimiento> movimientos)
        {
            return movimientos
                .GroupBy(m => m.Tipo)
                .ToDictionary(g => g.Key, g => g.Count());
        }
    }
}