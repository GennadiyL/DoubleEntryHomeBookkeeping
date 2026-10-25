namespace UnitTests.Shared;

/// <summary>
/// Pauses transfers until cancellation to verify processor token propagation.
/// The caller retains ownership of this in-memory stream.
/// </summary>
internal sealed class BlockingStorageStream : MemoryStream
{
	public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

	public override Task CopyToAsync(Stream destination, int bufferSize, CancellationToken cancellationToken)
	{
		return WaitForCancellation(cancellationToken);
	}

	public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken = default)
	{
		return new ValueTask(WaitForCancellation(cancellationToken));
	}

	private Task WaitForCancellation(CancellationToken cancellationToken)
	{
		Entered.TrySetResult();
		return Task.Delay(Timeout.Infinite, cancellationToken);
	}
}
