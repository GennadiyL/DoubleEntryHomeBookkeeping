using Shared.Contracts.Messaging;

namespace Shared.Contracts;

public interface IMessageService
{
	/// <summary>
	/// Publishes a message through the currently registered in-process or remote implementation.
	/// </summary>
	public Task PublishAsync<T>(T message, CancellationToken cancellationToken = default)
		where T : IMessage;
}
