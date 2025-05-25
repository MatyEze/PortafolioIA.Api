using Domain.ValueObjects;

namespace Domain.Entities;

public enum DataPointStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class DataPoint
{
    // Propiedades del agregado
    public Guid Id { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public FileMetadata File { get; private set; }
    public DataPointStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Colección de movimientos asociada
    private readonly List<Movimiento> _movements = new();
    public IReadOnlyCollection<Movimiento> Movements
        => _movements.AsReadOnly();

    // Constructor privado para EF Core
    private DataPoint() { }

    // Factory method para crear un nuevo DataPoint
    public static DataPoint Create(FileMetadata fileMeta)
    {
        return new DataPoint
        {
            Id = Guid.NewGuid(),
            CreatedAt = DateTimeOffset.UtcNow,
            File = fileMeta,
            Status = DataPointStatus.Pending
        };
    }

    // Transición a Processing
    public void StartProcessing()
    {
        if (Status != DataPointStatus.Pending)
            throw new InvalidOperationException("Solo PENDING puede pasar a PROCESSING.");
        Status = DataPointStatus.Processing;
    }

    // Añadir movimientos tras parseo
    public void AddMovements(IEnumerable<Movimiento> movimientos)
    {
        if (Status != DataPointStatus.Processing)
            throw new InvalidOperationException("Solo en PROCESSING se pueden añadir movimientos.");
        _movements.AddRange(movimientos);
    }

    // Marcar completado
    public void MarkCompleted()
    {
        if (Status != DataPointStatus.Processing)
            throw new InvalidOperationException("Solo PROCESSING puede pasar a COMPLETED.");
        if (!_movements.Any())
            throw new InvalidOperationException("No se puede completar sin movimientos.");
        Status = DataPointStatus.Completed;
    }

    // Marcar fallo
    public void MarkFailed(string error)
    {
        Status = DataPointStatus.Failed;
        ErrorMessage = error;
    }
}
