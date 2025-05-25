namespace Api.Endpoints.ProcessFile
{
    public class ProcessFileResponse
    {
        public Guid DataPointId { get; set; }
        public string Status { get; set; } = string.Empty;
        public string FileName { get; set; } = string.Empty;
        public string BrokerKey { get; set; } = string.Empty;
        public int MovimientosCount { get; set; }
        public long ProcessingTimeMs { get; set; }
        public DateTime ProcessedAt { get; set; }
        public List<string> Errores { get; set; } = new();
        public List<string> Advertencias { get; set; } = new();
        public ProcessingSummaryDto? Summary { get; set; }
    }

    public class ProcessingSummaryDto
    {
        public int TotalCompras { get; set; }
        public int TotalVentas { get; set; }
        public int TotalDepositos { get; set; }
        public int TotalExtracciones { get; set; }
        public int TotalDividendos { get; set; }
        public int TotalCauciones { get; set; }
        public int TotalOtros { get; set; }
        public decimal MontoTotalOperado { get; set; }
        public Dictionary<string, int> MovimientosPorTicker { get; set; } = new();
        public Dictionary<string, decimal> MontosPorMoneda { get; set; } = new();
        public DateTime FechaDesde { get; set; }
        public DateTime FechaHasta { get; set; }
    }
}