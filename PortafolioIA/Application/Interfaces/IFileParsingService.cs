using Domain.Entities;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Servicio para parsear archivos de diferentes brokers y generar movimientos
    /// </summary>
    public interface IFileParsingService
    {
        /// <summary>
        /// Parsea un archivo y genera una lista de movimientos
        /// </summary>
        /// <param name="fileStream">Stream del archivo a parsear</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <param name="brokerKey">Clave identificadora del broker (IOL, etc.)</param>
        /// <param name="dataPointId">ID del DataPoint asociado</param>
        /// <returns>Resultado del parsing con movimientos y estadísticas</returns>
        Task<ParsingResult> ParseFileAsync(Stream fileStream, string fileName, string brokerKey, Guid dataPointId);

        /// <summary>
        /// Verifica si el servicio puede parsear el archivo para el broker especificado
        /// </summary>
        /// <param name="brokerKey">Clave del broker</param>
        /// <param name="fileName">Nombre del archivo</param>
        /// <returns>True si puede parsear el archivo</returns>
        bool CanParse(string brokerKey, string fileName);

        /// <summary>
        /// Obtiene los brokers soportados por este parser
        /// </summary>
        /// <returns>Lista de claves de brokers soportados</returns>
        IEnumerable<string> GetSupportedBrokers();

        /// <summary>
        /// Obtiene las extensiones de archivo soportadas
        /// </summary>
        /// <returns>Lista de extensiones soportadas (.xlsx, .xls, etc.)</returns>
        IEnumerable<string> GetSupportedExtensions();
    }

    /// <summary>
    /// Resultado del procesamiento de un archivo
    /// </summary>
    public class ParsingResult
    {
        public List<Movimiento> Movimientos { get; set; } = new();
        public List<string> Errores { get; set; } = new();
        public List<string> Advertencias { get; set; } = new();
        public ParsingStatistics Statistics { get; set; } = new();

        public bool HasErrors => Errores.Count > 0;
        public bool HasWarnings => Advertencias.Count > 0;
        public bool IsSuccess => !HasErrors;
    }

    /// <summary>
    /// Estadísticas del procesamiento
    /// </summary>
    public class ParsingStatistics
    {
        public int TotalFilasProcessadas { get; set; }
        public int FilasExitosas { get; set; }
        public int FilasConErrores { get; set; }
        public int FilasIgnoradas { get; set; }
        public Dictionary<string, int> MovimientosPorTipo { get; set; } = new();
        public DateTime? FechaMasAntigua { get; set; }
        public DateTime? FechaMasReciente { get; set; }

        public double PorcentajeExito => TotalFilasProcessadas > 0
            ? (double)FilasExitosas / TotalFilasProcessadas * 100
            : 0;
    }
}