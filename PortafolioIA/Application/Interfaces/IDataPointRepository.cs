using Domain.Entities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Application.Interfaces
{
    /// <summary>
    /// Repositorio para gestión de DataPoints
    /// </summary>
    public interface IDataPointRepository
    {
        /// <summary>
        /// Agrega un nuevo DataPoint
        /// </summary>
        /// <param name="dataPoint">DataPoint a agregar</param>
        /// <returns>DataPoint agregado</returns>
        Task<DataPoint> AddAsync(DataPoint dataPoint);

        /// <summary>
        /// Obtiene un DataPoint por su ID incluyendo movimientos
        /// </summary>
        /// <param name="id">ID del DataPoint</param>
        /// <returns>DataPoint con movimientos o null si no existe</returns>
        Task<DataPoint?> GetByIdAsync(Guid id);

        /// <summary>
        /// Obtiene un DataPoint por su ID sin incluir movimientos
        /// </summary>
        /// <param name="id">ID del DataPoint</param>
        /// <returns>DataPoint sin movimientos o null si no existe</returns>
        Task<DataPoint?> GetByIdWithoutMovementsAsync(Guid id);

        /// <summary>
        /// Actualiza un DataPoint existente
        /// </summary>
        /// <param name="dataPoint">DataPoint a actualizar</param>
        Task UpdateAsync(DataPoint dataPoint);

        /// <summary>
        /// Obtiene todos los DataPoints con paginación
        /// </summary>
        /// <param name="pageNumber">Número de página (base 1)</param>
        /// <param name="pageSize">Tamaño de página</param>
        /// <returns>Lista paginada de DataPoints</returns>
        Task<(List<DataPoint> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize);

        /// <summary>
        /// Obtiene DataPoints por estado
        /// </summary>
        /// <param name="status">Estado a filtrar</param>
        /// <returns>Lista de DataPoints con el estado especificado</returns>
        Task<List<DataPoint>> GetByStatusAsync(DataPointStatus status);

        /// <summary>
        /// Obtiene DataPoints creados en un rango de fechas
        /// </summary>
        /// <param name="fromDate">Fecha desde</param>
        /// <param name="toDate">Fecha hasta</param>
        /// <returns>Lista de DataPoints en el rango especificado</returns>
        Task<List<DataPoint>> GetByDateRangeAsync(DateTimeOffset fromDate, DateTimeOffset toDate);

        /// <summary>
        /// Elimina un DataPoint y todos sus movimientos asociados
        /// </summary>
        /// <param name="id">ID del DataPoint a eliminar</param>
        /// <returns>True si se eliminó correctamente</returns>
        Task<bool> DeleteAsync(Guid id);

        /// <summary>
        /// Verifica si existe un DataPoint con el mismo archivo (nombre y tamaño)
        /// </summary>
        /// <param name="fileName">Nombre del archivo</param>
        /// <param name="fileSize">Tamaño del archivo</param>
        /// <returns>True si existe un DataPoint con el mismo archivo</returns>
        Task<bool> ExistsWithSameFileAsync(string fileName, long fileSize);

        /// <summary>
        /// Obtiene estadísticas generales de procesamiento
        /// </summary>
        /// <returns>Estadísticas de DataPoints</returns>
        Task<DataPointStatistics> GetStatisticsAsync();
    }

    /// <summary>
    /// Estadísticas de DataPoints
    /// </summary>
    public class DataPointStatistics
    {
        public int TotalDataPoints { get; set; }
        public int CompletedDataPoints { get; set; }
        public int ProcessingDataPoints { get; set; }
        public int FailedDataPoints { get; set; }
        public int PendingDataPoints { get; set; }
        public int TotalMovimientos { get; set; }
        public DateTime? OldestDataPoint { get; set; }
        public DateTime? NewestDataPoint { get; set; }
        public Dictionary<string, int> DataPointsByBroker { get; set; } = new();

        public double SuccessRate => TotalDataPoints > 0
            ? (double)CompletedDataPoints / TotalDataPoints * 100
            : 0;
    }
}