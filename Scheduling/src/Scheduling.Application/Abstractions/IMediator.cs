namespace Scheduling.Application.Abstractions;

/// <summary>
/// A deliberately tiny local mediator abstraction. The demo ships no MediatR dependency: the
/// contracts name <c>IMediator</c> and <c>IRequestHandler&lt;*, *&gt;</c>, and these are those names.
/// </summary>
public interface IRequest<out TResponse>;

/// <summary>Handles exactly one request type.</summary>
public interface IRequestHandler<in TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    /// <summary>Runs the request.</summary>
    Task<TResponse> HandleAsync(TRequest request, CancellationToken cancellationToken = default);
}

/// <summary>Finds the handler for a request and runs it.</summary>
public interface IMediator
{
    /// <summary>Dispatches <paramref name="request"/> to its handler.</summary>
    Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default);
}
