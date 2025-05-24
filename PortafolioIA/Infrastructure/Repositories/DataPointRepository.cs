using Application.Interfaces;
using Domain.Entities;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories
{
    public class DataPointRepository : IDataPointRepository
    {
        private readonly PortfolioDbContext _context;

        public DataPointRepository(PortfolioDbContext context)
        {
            _context = context;
        }

        public async Task<DataPoint> AddAsync(DataPoint dataPoint)
        {
            _context.DataPoints.Add(dataPoint);
            await _context.SaveChangesAsync();
            return dataPoint;
        }

        public async Task<DataPoint?> GetByIdAsync(Guid id)
        {
            return await _context.DataPoints
                .Include(dp => dp.Movements)
                .FirstOrDefaultAsync(dp => dp.Id == id);
        }

        public async Task<DataPoint?> GetByIdWithoutMovementsAsync(Guid id)
        {
            return await _context.DataPoints
                .FirstOrDefaultAsync(dp => dp.Id == id);
        }

        public async Task UpdateAsync(DataPoint dataPoint)
        {
            _context.DataPoints.Update(dataPoint);
            await _context.SaveChangesAsync();
        }

        public async Task<(List<DataPoint> Items, int TotalCount)> GetPagedAsync(int pageNumber, int pageSize)
        {
            var query = _context.DataPoints.AsQueryable();

            var totalCount = await query.CountAsync();

            var items = await query
                .OrderByDescending(dp => dp.CreatedAt)
                .Skip((pageNumber - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();

            return (items, totalCount);
        }

        public async Task<List<DataPoint>> GetByStatusAsync(DataPointStatus status)
        {
            return await _context.DataPoints
                .Where(dp => dp.Status == status)
                .OrderByDescending(dp => dp.CreatedAt)
                .ToListAsync();
        }

        public async Task<List<DataPoint>> GetByDateRangeAsync(DateTimeOffset fromDate, DateTimeOffset toDate)
        {
            return await _context.DataPoints
                .Where(dp => dp.CreatedAt >= fromDate && dp.CreatedAt <= toDate)
                .OrderByDescending(dp => dp.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> DeleteAsync(Guid id)
        {
            var dataPoint = await _context.DataPoints
                .Include(dp => dp.Movements)
                .FirstOrDefaultAsync(dp => dp.Id == id);

            if (dataPoint == null)
                return false;

            _context.DataPoints.Remove(dataPoint);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ExistsWithSameFileAsync(string fileName, long fileSize)
        {
            return await _context.DataPoints
                .AnyAsync(dp => dp.File.FileName == fileName && dp.File.SizeInBytes == fileSize);
        }

        public async Task<DataPointStatistics> GetStatisticsAsync()
        {
            var dataPoints = await _context.DataPoints.ToListAsync();
            var movimientosCount = await _context.Movimientos.CountAsync();

            var statistics = new DataPointStatistics
            {
                TotalDataPoints = dataPoints.Count,
                CompletedDataPoints = dataPoints.Count(dp => dp.Status == DataPointStatus.Completed),
                ProcessingDataPoints = dataPoints.Count(dp => dp.Status == DataPointStatus.Processing),
                FailedDataPoints = dataPoints.Count(dp => dp.Status == DataPointStatus.Failed),
                PendingDataPoints = dataPoints.Count(dp => dp.Status == DataPointStatus.Pending),
                TotalMovimientos = movimientosCount,
                OldestDataPoint = dataPoints.Any() ? dataPoints.Min(dp => dp.CreatedAt).DateTime : null,
                NewestDataPoint = dataPoints.Any() ? dataPoints.Max(dp => dp.CreatedAt).DateTime : null,
                DataPointsByBroker = new Dictionary<string, int>()
            };

            // Agrupar por broker (extraer de nombre de archivo o agregar campo Broker al DataPoint)
            //TODO: AGREGAR CAMPO BROKER AL DATAPOINT
            var brokerGroups = dataPoints
                .GroupBy(dp => ExtractBrokerFromFileName(dp.File.FileName))
                .ToDictionary(g => g.Key, g => g.Count());

            statistics.DataPointsByBroker = brokerGroups;

            return statistics;
        }

        private static string ExtractBrokerFromFileName(string fileName)
        {
            // Lógica simple para extraer broker del nombre del archivo
            // Esto puede mejorarse según las convenciones de nombres
            var upperFileName = fileName.ToUpperInvariant();

            if (upperFileName.Contains("IOL"))
                return "IOL";
            if (upperFileName.Contains("BALANZ"))
                return "BALANZ";
            if (upperFileName.Contains("BULL"))
                return "BULL";

            return "UNKNOWN";
        }
    }
}