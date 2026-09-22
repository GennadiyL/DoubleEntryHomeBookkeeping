using System.Collections.Concurrent;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Contracts.Messaging;

namespace Shared.Impl.Messaging;

/// <summary>
/// Defines the in-process message publisher.
/// Publishes messages to all matching handlers resolved from a fresh dependency injection scope.
/// SharedDiConfiguration registers one scoped publisher by default for local execution.
/// It depends only on shared messaging contracts and discovers business handlers through DI.
/// It has no compile-time reference to business-specific handler projects or a remote bus.
/// </summary>
internal class InProcessBusMessageService : IMessageService
{
	private readonly IServiceScopeFactory _scopeFactory;
	private readonly ConcurrentDictionary<Type, IHandlerWrapper> _wrappers = new();

	public InProcessBusMessageService(IServiceScopeFactory scopeFactory) => _scopeFactory = scopeFactory;

	public async Task PublishAsync<T>(T message)
		where T : IMessage
	{
		if (message == null)
		{
			throw new ArgumentNullException(nameof(message));
		}

		await PublishMessage(message);
	}

	protected virtual async Task PublishCore(IEnumerable<Func<IMessage, Task>> handlers, IMessage message)
	{
		List<Exception> exceptions = [];

		foreach (Func<IMessage, Task> handler in handlers)
		{
			try
			{
				await handler(message);
			}
			catch (AggregateException ex)
			{
				exceptions.AddRange(ex.Flatten().InnerExceptions);
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		}

		if (exceptions.Count > 0)
		{
			throw new AggregateException(exceptions);
		}
	}

	private async Task PublishMessage(IMessage message)
	{
		Type messageType = message.GetType();
		Func<Type, IHandlerWrapper> factory = CreateHandlerWrapper;
		IHandlerWrapper wrapper = _wrappers.GetOrAdd(messageType, factory);
		using IServiceScope scope = _scopeFactory.CreateScope();
		await wrapper.Handle(message, scope.ServiceProvider.GetService!, PublishCore);
	}

	private static IHandlerWrapper CreateHandlerWrapper(Type t)
	{
		object? wrapper = Activator.CreateInstance(typeof(HandlerWrapper<>).MakeGenericType(t));
		if (wrapper == null)
		{
			throw new InvalidOperationException($"Could not create wrapper for type {t}");
		}

		return (IHandlerWrapper)wrapper;
	}
}
