using FluentResults;

namespace Application.Common;

/// <summary>
/// Errores específicos del dominio de archivos y procesamiento
/// </summary>
public static class Errors
{
    /// <summary>
    /// Errores relacionados con archivos
    /// </summary>
    public static class File
    {
        public static Error NotFound(string fileName) =>
            new Error($"Archivo '{fileName}' no encontrado")
                .WithMetadata("ErrorCode", "FILE_NOT_FOUND")
                .WithMetadata("FileName", fileName);

        public static Error InvalidFormat(string fileName, string expectedFormat) =>
            new Error($"Archivo '{fileName}' no tiene el formato esperado '{expectedFormat}'")
                .WithMetadata("ErrorCode", "FILE_INVALID_FORMAT")
                .WithMetadata("FileName", fileName)
                .WithMetadata("ExpectedFormat", expectedFormat);

        public static Error TooLarge(long size, long maxSize) =>
            new Error($"Archivo demasiado grande. Tamaño: {size} bytes, Máximo permitido: {maxSize} bytes")
                .WithMetadata("ErrorCode", "FILE_TOO_LARGE")
                .WithMetadata("Size", size)
                .WithMetadata("MaxSize", maxSize);

        public static Error Empty() =>
            new Error("El archivo está vacío")
                .WithMetadata("ErrorCode", "FILE_EMPTY");

        public static Error AlreadyProcessed(string fileName) =>
            new Error($"El archivo '{fileName}' ya ha sido procesado anteriormente")
                .WithMetadata("ErrorCode", "FILE_ALREADY_PROCESSED")
                .WithMetadata("FileName", fileName);

        public static Error CorruptedOrUnreadable(string fileName) =>
            new Error($"El archivo '{fileName}' está corrupto o no se puede leer")
                .WithMetadata("ErrorCode", "FILE_CORRUPTED")
                .WithMetadata("FileName", fileName);
    }

    /// <summary>
    /// Errores relacionados con brokers
    /// </summary>
    public static class Broker
    {
        public static Error NotSupported(string brokerKey) =>
            new Error($"Broker '{brokerKey}' no es soportado")
                .WithMetadata("ErrorCode", "BROKER_NOT_SUPPORTED")
                .WithMetadata("BrokerKey", brokerKey);

        public static Error NoParserAvailable(string brokerKey, string fileName) =>
            new Error($"No hay parser disponible para el broker '{brokerKey}' y archivo '{fileName}'")
                .WithMetadata("ErrorCode", "BROKER_NO_PARSER")
                .WithMetadata("BrokerKey", brokerKey)
                .WithMetadata("FileName", fileName);
    }

    /// <summary>
    /// Errores relacionados con parsing
    /// </summary>
    public static class Parsing
    {
        public static Error InvalidHeader(string expectedHeader, string actualHeader) =>
            new Error($"Encabezado inválido. Esperado: '{expectedHeader}', Actual: '{actualHeader}'")
                .WithMetadata("ErrorCode", "PARSING_INVALID_HEADER")
                .WithMetadata("ExpectedHeader", expectedHeader)
                .WithMetadata("ActualHeader", actualHeader);

        public static Error InvalidRowData(int rowNumber, string reason) =>
            new Error($"Datos inválidos en fila {rowNumber}: {reason}")
                .WithMetadata("ErrorCode", "PARSING_INVALID_ROW")
                .WithMetadata("RowNumber", rowNumber)
                .WithMetadata("Reason", reason);

        public static Error NoValidMovements() =>
            new Error("No se encontraron movimientos válidos en el archivo")
                .WithMetadata("ErrorCode", "PARSING_NO_VALID_MOVEMENTS");

        public static Error GeneralError(string message, Exception? exception = null) =>
            new Error($"Error general de parsing: {message}")
                .WithMetadata("ErrorCode", "PARSING_GENERAL_ERROR")
                .WithMetadata("Exception", exception?.ToString() ?? "N/A");
    }

    /// <summary>
    /// Errores relacionados con DataPoints
    /// </summary>
    public static class DataPoint
    {
        public static Error NotFound(Guid id) =>
            new Error($"DataPoint con ID '{id}' no encontrado")
                .WithMetadata("ErrorCode", "DATAPOINT_NOT_FOUND")
                .WithMetadata("DataPointId", id);

        public static Error InvalidStatus(string currentStatus, string requiredStatus) =>
            new Error($"DataPoint tiene estado '{currentStatus}', se requiere '{requiredStatus}'")
                .WithMetadata("ErrorCode", "DATAPOINT_INVALID_STATUS")
                .WithMetadata("CurrentStatus", currentStatus)
                .WithMetadata("RequiredStatus", requiredStatus);

        public static Error CannotTransitionStatus(string fromStatus, string toStatus) =>
            new Error($"No se puede cambiar el estado de '{fromStatus}' a '{toStatus}'")
                .WithMetadata("ErrorCode", "DATAPOINT_INVALID_TRANSITION")
                .WithMetadata("FromStatus", fromStatus)
                .WithMetadata("ToStatus", toStatus);
    }

    /// <summary>
    /// Errores relacionados con movimientos financieros
    /// </summary>
    public static class Movement
    {
        public static Error InvalidAmount(decimal amount) =>
            new Error($"Cantidad inválida: {amount}")
                .WithMetadata("ErrorCode", "MOVEMENT_INVALID_AMOUNT")
                .WithMetadata("Amount", amount);

        public static Error InvalidPrice(decimal price) =>
            new Error($"Precio inválido: {price}")
                .WithMetadata("ErrorCode", "MOVEMENT_INVALID_PRICE")
                .WithMetadata("Price", price);

        public static Error MissingTicker(string movementType) =>
            new Error($"Ticker requerido para movimiento de tipo '{movementType}'")
                .WithMetadata("ErrorCode", "MOVEMENT_MISSING_TICKER")
                .WithMetadata("MovementType", movementType);

        public static Error InvalidDate(string dateValue) =>
            new Error($"Fecha inválida: '{dateValue}'")
                .WithMetadata("ErrorCode", "MOVEMENT_INVALID_DATE")
                .WithMetadata("DateValue", dateValue);
    }

    /// <summary>
    /// Errores de validación general
    /// </summary>
    public static class Validation
    {
        public static Error Required(string fieldName) =>
            new Error($"El campo '{fieldName}' es requerido")
                .WithMetadata("ErrorCode", "VALIDATION_REQUIRED")
                .WithMetadata("FieldName", fieldName);

        public static Error InvalidLength(string fieldName, int currentLength, int maxLength) =>
            new Error($"El campo '{fieldName}' excede la longitud máxima. Actual: {currentLength}, Máximo: {maxLength}")
                .WithMetadata("ErrorCode", "VALIDATION_INVALID_LENGTH")
                .WithMetadata("FieldName", fieldName)
                .WithMetadata("CurrentLength", currentLength)
                .WithMetadata("MaxLength", maxLength);

        public static Error InvalidFormat(string fieldName, string expectedFormat) =>
            new Error($"El campo '{fieldName}' no tiene el formato esperado: '{expectedFormat}'")
                .WithMetadata("ErrorCode", "VALIDATION_INVALID_FORMAT")
                .WithMetadata("FieldName", fieldName)
                .WithMetadata("ExpectedFormat", expectedFormat);
    }

    /// <summary>
    /// Errores de base de datos
    /// </summary>
    public static class Database
    {
        public static Error SaveFailed(string entity, Exception? exception = null) =>
            new Error($"Error al guardar {entity} en la base de datos")
                .WithMetadata("ErrorCode", "DATABASE_SAVE_FAILED")
                .WithMetadata("Entity", entity)
                .WithMetadata("Exception", exception?.ToString() ?? "N/A");

        public static Error ConnectionFailed(Exception? exception = null) =>
            new Error("Error de conexión a la base de datos")
                .WithMetadata("ErrorCode", "DATABASE_CONNECTION_FAILED")
                .WithMetadata("Exception", exception?.ToString() ?? "N/A");

        public static Error ConcurrencyConflict(string entity) =>
            new Error($"Conflicto de concurrencia al actualizar {entity}")
                .WithMetadata("ErrorCode", "DATABASE_CONCURRENCY_CONFLICT")
                .WithMetadata("Entity", entity);
    }
}