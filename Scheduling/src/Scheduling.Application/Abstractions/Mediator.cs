using System.Collections.Concurrent;
using System.Reflection;

namespace Scheduling.Application.Abstractions;

/// <summary>
/// Resolves <c>IRequestHandler&lt;TRequest, TResponse&gt;</c> from the container and invokes it.
/// Forty lines instead of a package: everything this demo needs a mediator for.
/// </summary>
public sealed class Mediator(IServiceProvider services) : IMediator
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> Handlers = new();

    private readonly IServiceProvider _services = services;

    /// <inheritdoc/>
    public Task<TResponse> SendAsync<TResponse>(IRequest<TResponse> request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        Type handlerType = typeof(IRequestHandler<,>).MakeGenericType(request.GetType(), typeof(TResponse));
        object handler = _services.GetService(handlerType)
            ?? throw new InvalidOperationException($"No handler is registered for {request.GetType().Name}.");

        MethodInfo method = Handlers.GetOrAdd(
            handlerType,
            static type => type.GetMethod(nameof(IRequestHandler<IRequest<object>, object>.HandleAsync))
                ?? throw new InvalidOperationException($"{type.Name} has no HandleAsync method."));

        return (Task<TResponse>)method.Invoke(handler, [request, cancellationToken])!;
    }
}
