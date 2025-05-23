using System;

namespace Domain.Entities
{
    public enum TipoMoneda
    {
        PesoArgentino,
        DolarEstadounidense,
        Euro,
        Real,
        Otro
    }

    public class Divisa
    {
        public Guid Id { get; private set; }
        public TipoMoneda Tipo { get; private set; }
        public string Codigo { get; private set; } // ARS, USD, EUR, etc.
        public string Simbolo { get; private set; } // $, US$, €, etc.
        public string Descripcion { get; private set; }
        public decimal Cantidad { get; private set; }
        public DateTimeOffset CreatedAt { get; private set; }
        public DateTimeOffset? UpdatedAt { get; private set; }

        // Constructor privado para EF Core
        private Divisa() { }

        // Factory method para crear una nueva Divisa
        public static Divisa Create(TipoMoneda tipo, string codigo, string simbolo, string descripcion)
        {
            if (string.IsNullOrWhiteSpace(codigo))
                throw new ArgumentException("El código no puede estar vacío", nameof(codigo));

            if (string.IsNullOrWhiteSpace(simbolo))
                throw new ArgumentException("El símbolo no puede estar vacío", nameof(simbolo));

            if (string.IsNullOrWhiteSpace(descripcion))
                throw new ArgumentException("La descripción no puede estar vacía", nameof(descripcion));

            return new Divisa
            {
                Id = Guid.NewGuid(),
                Tipo = tipo,
                Codigo = codigo.ToUpper(),
                Simbolo = simbolo,
                Descripcion = descripcion,
                Cantidad = 0,
                CreatedAt = DateTimeOffset.UtcNow
            };
        }

        // Agregar cantidad (depósitos, ganancias)
        public void AgregarCantidad(decimal cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a agregar debe ser positiva", nameof(cantidad));

            Cantidad += cantidad;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Reducir cantidad (extracciones, gastos)
        public void ReducirCantidad(decimal cantidad)
        {
            if (cantidad <= 0)
                throw new ArgumentException("La cantidad a reducir debe ser positiva", nameof(cantidad));

            if (Cantidad < cantidad)
                throw new InvalidOperationException($"Fondos insuficientes. Disponible: {Cantidad}, Intentando usar: {cantidad}");

            Cantidad -= cantidad;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Actualizar cantidad directamente (para ajustes)
        public void ActualizarCantidad(decimal nuevaCantidad)
        {
            if (nuevaCantidad < 0)
                throw new ArgumentException("La cantidad no puede ser negativa", nameof(nuevaCantidad));

            Cantidad = nuevaCantidad;
            UpdatedAt = DateTimeOffset.UtcNow;
        }

        // Método helper para obtener descripción formateada
        public string ObtenerDescripcionCompleta()
        {
            return $"{Descripcion} ({Codigo})";
        }

        // Método helper para formatear cantidad con símbolo
        public string FormatearCantidad()
        {
            return $"{Simbolo} {Cantidad:N2}";
        }
    }
}