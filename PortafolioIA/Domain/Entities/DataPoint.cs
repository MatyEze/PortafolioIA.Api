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
    public Guid UserId { get; private set; } // Nueva propiedad
    public DateTimeOffset CreatedAt { get; private set; }
    public FileMetadata File { get; private set; }
    public DataPointStatus Status { get; private set; }
    public string? ErrorMessage { get; private set; }

    // Colección de movimientos asociada
    private readonly List<Movimiento> _movements = new();
    public IReadOnlyCollection<Movimiento> Movements
        => _movements.AsReadOnly();

    // Navigation properties
    public User User { get; private set; } // Nueva navigation property

    // Constructor privado para EF Core
    private DataPoint() { }

    // Factory method para crear un nuevo DataPoint - ACTUALIZADO
    public static DataPoint Create(Guid userId, FileMetadata fileMeta)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId no puede estar vacío", nameof(userId));

        return new DataPoint
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreatedAt = DateTimeOffset.UtcNow,
            File = fileMeta,
            Status = DataPointStatus.Pending
        };
    }

    // Factory method manteniendo compatibilidad hacia atrás
    public static DataPoint Create(FileMetadata fileMeta)
    {
        // Temporalmente crear sin usuario hasta que se migre todo el código
        return new DataPoint
        {
            Id = Guid.NewGuid(),
            UserId = Guid.Empty, // Temporal
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

    // Verificar si el usuario es propietario
    public bool BelongsToUser(Guid userId)
    {
        return UserId == userId;
    }

    // Método para asociar a un usuario (para migración)
    public void AssignToUser(Guid userId)
    {
        if (userId == Guid.Empty)
            throw new ArgumentException("UserId no puede estar vacío", nameof(userId));

        UserId = userId;
    }
}