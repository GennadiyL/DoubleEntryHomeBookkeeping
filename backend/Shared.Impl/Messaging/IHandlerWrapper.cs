using Shared.Contracts.Messaging;

namespace Shared.Impl.Messaging;

internal interface IHandlerWrapper
{
	public Task Handle(
		IMessage message,
		ServiceFactory serviceFactory,
		Func<IEnumerable<Func<IMessage, Task>>, IMessage, Task> publish);
}
