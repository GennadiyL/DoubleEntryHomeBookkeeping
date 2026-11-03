namespace UnitTests.Shared;

/// <summary>
/// Pauses a real stream transfer after bytes have reached its destination.
/// Source copies pause after the first byte to expose incomplete candidate files.
/// Destination writes pause while the storage reader still owns its file handle.
/// Tests release the transfer explicitly or cancel its supplied token.
/// An optional failure exercises cleanup after a partially completed copy.
/// Remaining bytes use the normal MemoryStream implementation.
/// Completion signals avoid timing-dependent sleeps in concurrency tests.
/// The test owns and disposes this stream.
/// </summary>
internal sealed class PausingStorageStream : MemoryStream
{
	public PausingStorageStream(byte[] content)
	{
		Write(content);
		Position = 0;
	}

	public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	public Exception? Failure { get; set; }

	public override async Task CopyToAsync(
		Stream destination,
		int bufferSize,
		CancellationToken cancellationToken)
	{
		byte[] first = new byte[1];
		int count = await base.ReadAsync(first.AsMemory(), cancellationToken);
		await destination.WriteAsync(first.AsMemory(0, count), cancellationToken);
		await Pause(cancellationToken);
		await base.CopyToAsync(destination, bufferSize, cancellationToken);
	}

	public override async ValueTask WriteAsync(
		ReadOnlyMemory<byte> buffer,
		CancellationToken cancellationToken = default)
	{
		await base.WriteAsync(buffer, cancellationToken);
		await Pause(cancellationToken);
	}

	private async Task Pause(CancellationToken cancellationToken)
	{
		Entered.TrySetResult();
		await Release.Task.WaitAsync(cancellationToken);
		if (Failure is not null)
		{
			throw Failure;
		}
	}
}
