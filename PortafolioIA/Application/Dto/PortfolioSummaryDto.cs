using Domain.Entities;
using System;
using System.Collections.Generic;

namespace Application.Dto
{
    /// <summary>
    /// DTO para resúmenes del portfolio - usado por servicios internos
    /// No relacionado con endpoints HTTP específicos
    /// </summary>
    public class PortfolioSummaryDto
    {
        public List<ActivoPositionDto> Activos { get; set; } = new();
        public List<DivisaPositionDto> Divisas { get; set; } = new();
        public decimal ValorTotalPortfolio { get; set; }
        public DateTime FechaActualizacion { get; set; }
        public PortfolioMetricsDto Metricas { get; set; } = new();
    }

    public class ActivoPositionDto
    {
        public string Ticker { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public TipoActivo Tipo { get; set; }
        public int Cantidad { get; set; }
        public decimal PrecioPromedio { get; set; }
        public decimal ValorTotal { get; set; }
        public decimal PorcentajePortfolio { get; set; }
    }

    public class DivisaPositionDto
    {
        public TipoMoneda Tipo { get; set; }
        public string Codigo { get; set; } = string.Empty;
        public string Simbolo { get; set; } = string.Empty;
        public decimal Cantidad { get; set; }
        public decimal PorcentajePortfolio { get; set; }
    }

    public class PortfolioMetricsDto
    {
        public int TotalActivos { get; set; }
        public int TotalDivisas { get; set; }
        public int TotalMovimientos { get; set; }
        public decimal TotalComisionesImpuestos { get; set; }
        public Dictionary<TipoActivo, int> ActivosPorTipo { get; set; } = new();
        public Dictionary<TipoMovimiento, int> MovimientosPorTipo { get; set; } = new();
        public DateTime FechaMovimientoMasAntiguo { get; set; }
        public DateTime FechaMovimientoMasReciente { get; set; }
    }
}