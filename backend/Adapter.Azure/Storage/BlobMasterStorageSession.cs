using Shared.Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Master.Contracts.Adapters.MasterStorage;
using Microsoft.Extensions.Logging;
using Shared.Contracts;

namespace Adapter.Azure.Storage;

/// <summary>
/// Owns a temporary Master database and its publication to one leased Azure blob.
/// The downloaded database path is used only by the caller's persistence resources.
/// Publication accepts a separate completed snapshot and keeps it read-locked during upload.
/// Download and upload combine caller cancellation with the acquired handle's ownership token.
/// Failed publication invalidates the session to prevent reuse after an uncertain outcome.
/// Disposal waits for publication and removes only this session's temporary directory.
/// Cleanup failures are logged without hiding an earlier processing exception.
/// The caller closes database connections before disposal and separately releases its access handle.
/// </summary>
internal sealed class BlobMasterStorageSession : IMasterStorageSession
{
	private readonly ISharedContext _sharedContext;
	private readonly BlobClient _blob;
	private readonly BlobStorageLock _access;
	private readonly DirectoryInfo _directory;
	private readonly ILogger _logger;
	private readonly SemaphoreSlim _gate = new(1, 1);
	private bool _disposed;
	private bool _faulted;
	private static readonly Action<ILogger, Exception?> CleanupFailed = LoggerMessage.Define(
		LogLevel.Warning, new EventId(3, nameof(CleanupFailed)), "Master storage temporary directory cleanup failed.");

	internal BlobMasterStorageSession(
		ISharedContext sharedContext,
		BlobClient blob,
		BlobStorageLock access,
		DirectoryInfo directory,
		ILogger logger)
	{
		_sharedContext = sharedContext;
		_blob = blob;
		_access = access;
		_directory = directory;
		_logger = logger;
		StoragePath = Path.Combine(directory.FullName, "master.db");
	}

	public string StoragePath { get; }

	internal async Task Download(CancellationToken cancellationToken)
	{
		using CancellationTokenSource cancellation = LinkCancellation(cancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();
		BlobDownloadToOptions options = new() { Conditions = LeaseConditions() };
		await using FileStream destination = _sharedContext.FileService.Open(StoragePath, new FileStreamOptions
		{
			Mode = FileMode.CreateNew, Access = FileAccess.Write, Share = FileShare.None,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
		await _blob.DownloadToAsync(destination, options, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public async Task Publish(string snapshotPath, CancellationToken cancellationToken = default)
	{
		await _gate.WaitAsync(cancellationToken);
		try
		{
			EnsureUsable();
			await UploadSnapshot(snapshotPath, cancellationToken);
		}
		catch
		{
			_faulted = true;
			throw;
		}
		finally
		{
			_gate.Release();
		}
	}

	private async Task UploadSnapshot(string snapshotPath, CancellationToken cancellationToken)
	{
		ValidateSnapshotPath(snapshotPath);
		using CancellationTokenSource cancellation = LinkCancellation(cancellationToken);
		cancellation.Token.ThrowIfCancellationRequested();
		await using FileStream snapshot = _sharedContext.FileService.Open(snapshotPath, new FileStreamOptions
		{
			Mode = FileMode.Open,
			Access = FileAccess.Read,
			Share = FileShare.Read,
			BufferSize = 81920,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
		BlobUploadOptions options = new()
		{
			Conditions = LeaseConditions()
		};
		await _blob.UploadAsync(snapshot, options, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	private void ValidateSnapshotPath(string snapshotPath)
	{
		ArgumentException.ThrowIfNullOrWhiteSpace(snapshotPath);
		StringComparison comparison = OperatingSystem.IsWindows() ? StringComparison.OrdinalIgnoreCase : StringComparison.Ordinal;
		if (string.Equals(Path.GetFullPath(snapshotPath), StoragePath, comparison))
		{
			throw new ArgumentException("Publication requires a separate completed database snapshot.", nameof(snapshotPath));
		}
	}

	private void EnsureUsable()
	{
		ObjectDisposedException.ThrowIf(_disposed, this);
		if (_faulted)
		{
			throw new InvalidOperationException("The storage session failed. Reopen the Master database before retrying.");
		}
	}

	private CancellationTokenSource LinkCancellation(CancellationToken cancellationToken) =>
		CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, _access.CancellationToken);

	private BlobRequestConditions LeaseConditions() => new() { LeaseId = _access.LeaseId };

	public async ValueTask DisposeAsync()
	{
		await _gate.WaitAsync();
		try
		{
			if (_disposed)
			{
				return;
			}
			_disposed = true;
			DeleteTemporaryDirectory();
		}
		finally
		{
			_gate.Release();
		}
	}

	private void DeleteTemporaryDirectory()
	{
		try
		{
			if (_sharedContext.DirectoryService.Exists(_directory.FullName))
			{
				_sharedContext.DirectoryService.Delete(_directory.FullName, true);
			}
		}
		catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
		{
			CleanupFailed(_logger, exception);
		}
	}
}
