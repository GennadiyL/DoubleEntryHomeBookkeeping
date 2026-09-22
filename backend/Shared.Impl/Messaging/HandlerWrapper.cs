using Shared.Contracts.Messaging;

namespace Shared.Impl.Messaging;

/// <summary>
/// Defines the typed message-handler wrapper.
/// Adapts an untyped message dispatch request to all registered handlers of one message type.
/// The in-process publisher caches a wrapper per runtime message type.
/// It resolves IMessageHandler<T> instances from the publisher-created service scope.
/// It does not select transports or retain resolved scoped handlers.
/// </summary>
internal class HandlerWrapper<T> : IHandlerWrapper
	where T : IMessage
{
	public async Task Handle(
		IMessage message,
		ServiceFactory serviceFactory,
		Func<IEnumerable<Func<IMessage, Task>>, IMessage, Task> publish)
	{
		IEnumerable<Func<IMessage, Task>> handlers = serviceFactory.GetInstances<IMessageHandler<T>>().Select(CreateAsyncHandleFunc);

		await publish(handlers, message);
	}

	private static Func<IMessage, Task> CreateAsyncHandleFunc(IMessageHandler<T> handler) =>
		message => handler.Handle((T)message);
}
