using MediatR;
using FluentResults;

namespace Application.Common.Abstractions;

/// <summary>
/// Interface base para Commands que no retornan valor
/// </summary>
public interface ICommand : IRequest<Result>
{
}

/// <summary>
/// Interface base para Commands que retornan un valor
/// </summary>
/// <typeparam name="TResponse">Tipo de respuesta del command</typeparam>
public interface ICommand<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Interface base para Queries
/// </summary>
/// <typeparam name="TResponse">Tipo de respuesta de la query</typeparam>
public interface IQuery<TResponse> : IRequest<Result<TResponse>>
{
}

/// <summary>
/// Interface base para handlers de Commands sin retorno
/// </summary>
/// <typeparam name="TCommand">Tipo del command</typeparam>
public interface ICommandHandler<TCommand> : IRequestHandler<TCommand, Result>
    where TCommand : ICommand
{
}

/// <summary>
/// Interface base para handlers de Commands con retorno
/// </summary>
/// <typeparam name="TCommand">Tipo del command</typeparam>
/// <typeparam name="TResponse">Tipo de respuesta</typeparam>
public interface ICommandHandler<TCommand, TResponse> : IRequestHandler<TCommand, Result<TResponse>>
    where TCommand : ICommand<TResponse>
{
}

/// <summary>
/// Interface base para handlers de Queries
/// </summary>
/// <typeparam name="TQuery">Tipo de la query</typeparam>
/// <typeparam name="TResponse">Tipo de respuesta</typeparam>
public interface IQueryHandler<TQuery, TResponse> : IRequestHandler<TQuery, Result<TResponse>>
    where TQuery : IQuery<TResponse>
{
}