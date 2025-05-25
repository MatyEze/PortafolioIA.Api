using System;

namespace Domain.Entities
{
    public enum TipoActivo
    {
        Accion,
        Cedear,
        Bono,
        FCI,
        ETF,
        Otro
    }

    public class Activo
    {
        public Guid Id { get; private set; }
        public string Ticker { get; private set; }
        public string Nombre { get; private set; }
        public TipoActivo Tipo { get; private set; }
        public int Cantidad { get; private set; }
        public decimal PrecioPromedio { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        // Constructor privado para EF Core
        private Activo() { }

        // Factory method para crear un nuevo Activo
        public static Activo Create(string ticker, string nombre, TipoActivo tipo)
        {
            if (string.IsNullOrWhiteSpace(ticker))
                throw new ArgumentException("El ticker no puede estar vacío", nameof(ticker));

            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacío", nameof(nombre));

            return new Activo
            {
                Id = Guid.NewGuid(),
                Ticker = ticker.ToUpper(),
                Nombre = nombre,
                Tipo = tipo,
                Cantidad = 0,
                PrecioPromedio = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        // Actualizar posición del activo (compra/venta)
        public void ActualizarPosicion(int cantidad, decimal precio)
        {
            if (precio < 0)
                throw new ArgumentException("El precio no puede ser negativo", nameof(precio));

            var nuevaCantidad = Cantidad + cantidad;

            if (nuevaCantidad < 0)
                throw new InvalidOperationException($"No se puede vender más cantidad de la disponible. Disponible: {Cantidad}, Intentando vender: {Math.Abs(cantidad)}");

            // Solo actualizar precio promedio en compras (cantidad positiva) y cuando hay cantidad resultante
            if (cantidad > 0 && nuevaCantidad > 0)
            {
                PrecioPromedio = ((PrecioPromedio * Cantidad) + (precio * cantidad)) / nuevaCantidad;
            }

            Cantidad = nuevaCantidad;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Actualizar información del activo
        public void ActualizarInformacion(string nombre, TipoActivo tipo)
        {
            if (string.IsNullOrWhiteSpace(nombre))
                throw new ArgumentException("El nombre no puede estar vacío", nameof(nombre));

            Nombre = nombre;
            Tipo = tipo;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Obtener valor total de la posición
        public decimal ObtenerValorTotal()
        {
            return Cantidad * PrecioPromedio;
        }
    }
}