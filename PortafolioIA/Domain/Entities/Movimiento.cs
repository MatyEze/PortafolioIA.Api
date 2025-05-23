using System;

namespace Domain.Entities
{
    public enum TipoMovimiento
    {
        Compra,
        Venta,
        Deposito,
        Extraccion,
        Dividendos,
        Caucion,
        LiquidacionCaucion,
        SuscripcionFondo,
        RescateFondo,
        Credito,
        Otro
    }

    public class Movimiento
    {
        public Guid Id { get; private set; }
        public Guid DataPointId { get; private set; }
        public int NumeroMovimiento { get; private set; }
        public string Broker { get; private set; }
        public string? Ticker { get; private set; }
        public TipoMovimiento Tipo { get; private set; }
        public DateTime FechaConcertacion { get; private set; }
        public DateTime FechaLiquidacion { get; private set; }
        public int Cantidad { get; private set; }
        public decimal Precio { get; private set; }
        public decimal Comision { get; private set; }
        public decimal IvaComision { get; private set; }
        public decimal OtrosImpuestos { get; private set; }
        public decimal MontoTotal { get; private set; }
        public TipoMoneda Moneda { get; private set; }
        public string? Observaciones { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }

        // Navigation property
        public DataPoint DataPoint { get; private set; }

        // Constructor privado para EF Core
        private Movimiento() { }

        // Factory method para crear un nuevo Movimiento
        public static Movimiento Create(
            Guid dataPointId,
            int numeroMovimiento,
            string broker,
            TipoMovimiento tipo,
            DateTime fechaConcertacion,
            DateTime fechaLiquidacion,
            int cantidad,
            decimal precio,
            decimal comision,
            decimal montoTotal,
            TipoMoneda moneda,
            string? ticker = null,
            decimal ivaComision = 0,
            decimal otrosImpuestos = 0,
            string? observaciones = null)
        {
            if (dataPointId == Guid.Empty)
                throw new ArgumentException("DataPointId no puede estar vacío", nameof(dataPointId));

            if (string.IsNullOrWhiteSpace(broker))
                throw new ArgumentException("El broker no puede estar vacío", nameof(broker));

            if (numeroMovimiento <= 0)
                throw new ArgumentException("El número de movimiento debe ser positivo", nameof(numeroMovimiento));

            // Validaciones específicas por tipo de movimiento
            ValidarTipoMovimiento(tipo, ticker, cantidad, precio);

            return new Movimiento
            {
                Id = Guid.NewGuid(),
                DataPointId = dataPointId,
                NumeroMovimiento = numeroMovimiento,
                Broker = broker,
                Ticker = ticker?.ToUpper(),
                Tipo = tipo,
                FechaConcertacion = fechaConcertacion,
                FechaLiquidacion = fechaLiquidacion,
                Cantidad = cantidad,
                Precio = precio,
                Comision = comision,
                IvaComision = ivaComision,
                OtrosImpuestos = otrosImpuestos,
                MontoTotal = montoTotal,
                Moneda = moneda,
                Observaciones = observaciones,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        // Actualizar observaciones
        public void AgregarObservacion(string observacion)
        {
            if (string.IsNullOrWhiteSpace(observacion))
                return;

            if (string.IsNullOrEmpty(Observaciones))
            {
                Observaciones = observacion;
            }
            else
            {
                Observaciones += $" | {observacion}";
            }
        }

        // Calcular monto neto (sin comisiones)
        public decimal ObtenerMontoNeto()
        {
            return MontoTotal - Comision - IvaComision - OtrosImpuestos;
        }

        // Obtener total de comisiones e impuestos
        public decimal ObtenerTotalComisionesImpuestos()
        {
            return Comision + IvaComision + OtrosImpuestos;
        }

        // Validar que el movimiento sea consistente según su tipo
        private static void ValidarTipoMovimiento(TipoMovimiento tipo, string? ticker, int cantidad, decimal precio)
        {
            switch (tipo)
            {
                case TipoMovimiento.Compra:
                case TipoMovimiento.Venta:
                    if (string.IsNullOrWhiteSpace(ticker))
                        throw new ArgumentException("Las operaciones de compra/venta requieren ticker", nameof(ticker));
                    if (cantidad <= 0)
                        throw new ArgumentException("La cantidad debe ser positiva para compra/venta", nameof(cantidad));
                    if (precio < 0)
                        throw new ArgumentException("El precio no puede ser negativo", nameof(precio));
                    break;

                case TipoMovimiento.Deposito:
                case TipoMovimiento.Extraccion:
                case TipoMovimiento.Dividendos:
                    // Estos movimientos pueden no tener ticker
                    break;

                case TipoMovimiento.Caucion:
                case TipoMovimiento.LiquidacionCaucion:
                    if (cantidad <= 0)
                        throw new ArgumentException("La cantidad debe ser positiva para cauciones", nameof(cantidad));
                    break;
            }
        }

        // Determinar si es un movimiento de entrada o salida de dinero
        public bool EsEntradaDinero()
        {
            return Tipo switch
            {
                TipoMovimiento.Venta => true,
                TipoMovimiento.Deposito => true,
                TipoMovimiento.Dividendos => true,
                TipoMovimiento.LiquidacionCaucion => true,
                TipoMovimiento.RescateFondo => true,
                TipoMovimiento.Credito => true,
                _ => false
            };
        }

        // Determinar si es un movimiento de salida de dinero
        public bool EsSalidaDinero()
        {
            return Tipo switch
            {
                TipoMovimiento.Compra => true,
                TipoMovimiento.Extraccion => true,
                TipoMovimiento.Caucion => true,
                TipoMovimiento.SuscripcionFondo => true,
                _ => false
            };
        }
    }
}