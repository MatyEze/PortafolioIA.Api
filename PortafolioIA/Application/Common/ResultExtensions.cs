using FluentResults;

namespace Application.Common;

/// <summary>
/// Alias y extensiones para FluentResults.Result
/// Usamos directamente FluentResults sin wrappers para evitar conflictos de tipos
/// </summary>
public static class ResultExtensions
{
    /// <summary>
    /// Crea un resultado exitoso
    /// </summary>
    public static Result Success() => Result.Ok();

    /// <summary>
    /// Crea un resultado exitoso con valor
    /// </summary>
    public static Result<T> Success<T>(T value) => Result.Ok(value);

    /// <summary>
    /// Crea un resultado con error
    /// </summary>
    public static Result Failure(string error) => Result.Fail(error);

    /// <summary>
    /// Crea un resultado con error específico
    /// </summary>
    public static Result Failure(IError error) => Result.Fail(error);

    /// <summary>
    /// Crea un resultado con múltiples errores
    /// </summary>
    public static Result Failure(IEnumerable<string> errors) => Result.Fail(errors);

    /// <summary>
    /// Crea un resultado con múltiples errores específicos
    /// </summary>
    public static Result Failure(IEnumerable<IError> errors) => Result.Fail(errors);

    /// <summary>
    /// Crea un resultado con error para tipo genérico
    /// </summary>
    public static Result<T> Failure<T>(string error) => Result.Fail<T>(error);

    /// <summary>
    /// Crea un resultado con error específico para tipo genérico
    /// </summary>
    public static Result<T> Failure<T>(IError error) => Result.Fail<T>(error);

    /// <summary>
    /// Crea un resultado con múltiples errores para tipo genérico
    /// </summary>
    public static Result<T> Failure<T>(IEnumerable<string> errors) => Result.Fail<T>(errors);

    /// <summary>
    /// Crea un resultado con múltiples errores específicos para tipo genérico
    /// </summary>
    public static Result<T> Failure<T>(IEnumerable<IError> errors) => Result.Fail<T>(errors);

    /// <summary>
    /// Convierte un valor en un Result exitoso
    /// </summary>
    public static Result<T> ToResult<T>(this T value) => Result.Ok(value);

    /// <summary>
    /// Ejecuta una acción si el resultado es exitoso
    /// </summary>
    public static Result<T> OnSuccess<T>(this Result<T> result, Action<T> action)
    {
        if (result.IsSuccess)
            action(result.Value);
        return result;
    }

    /// <summary>
    /// Ejecuta una acción si el resultado es exitoso (sin valor)
    /// </summary>
    public static Result OnSuccess(this Result result, Action action)
    {
        if (result.IsSuccess)
            action();
        return result;
    }

    /// <summary>
    /// Ejecuta una acción si el resultado tiene errores
    /// </summary>
    public static Result<T> OnFailure<T>(this Result<T> result, Action<List<IError>> action)
    {
        if (result.IsFailed)
            action(result.Errors);
        return result;
    }

    /// <summary>
    /// Ejecuta una acción si el resultado tiene errores (sin valor)
    /// </summary>
    public static Result OnFailure(this Result result, Action<List<IError>> action)
    {
        if (result.IsFailed)
            action(result.Errors);
        return result;
    }

    /// <summary>
    /// Transforma el valor de un Result exitoso
    /// </summary>
    public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> transform)
    {
        return result.IsSuccess ?
            Result.Ok(transform(result.Value)) :
            Result.Fail<TOut>(result.Errors);
    }

    /// <summary>
    /// Aplica una función que retorna un Result sobre un Result exitoso
    /// </summary>
    public static Result<TOut> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Result<TOut>> bind)
    {
        return result.IsSuccess ?
            bind(result.Value) :
            Result.Fail<TOut>(result.Errors);
    }

    /// <summary>
    /// Convierte un Result sin valor a Result<T> con error
    /// </summary>
    public static Result<T> ToResult<T>(this Result result)
    {
        return result.IsSuccess ?
            Result.Ok<T>(default(T)!) :
            Result.Fail<T>(result.Errors);
    }

    /// <summary>
    /// Obtiene el primer mensaje de error o string vacío
    /// </summary>
    public static string GetFirstErrorMessage(this Result result)
    {
        return result.Errors.FirstOrDefault()?.Message ?? string.Empty;
    }

    /// <summary>
    /// Obtiene todos los mensajes de error concatenados
    /// </summary>
    public static string GetAllErrorMessages(this Result result, string separator = "; ")
    {
        return string.Join(separator, result.Errors.Select(e => e.Message));
    }
}