namespace Shared.Contracts.Messaging;

public interface IMessage
{
	public Guid MessageId { get; }
	public Guid CorrelationId { get; }
}
