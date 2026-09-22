namespace Shared.Contracts.Messaging;

public interface IMessageHandler<in T>
	where T : IMessage
{
	public Task Handle(T message);
}
