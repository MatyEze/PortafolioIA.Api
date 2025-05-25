namespace Domain.ValueObjects;

public record FileMetadata(
    string FileName,
    long SizeInBytes,
    string ContentType
);
