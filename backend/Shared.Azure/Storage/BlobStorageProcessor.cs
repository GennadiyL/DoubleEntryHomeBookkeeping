using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Shared.Contracts.Storage;

namespace Shared.Azure.Storage;

/// <summary>
/// Stores objects in the Azure Blob container supplied by its factory.
/// Streams content without taking ownership of caller-provided streams or locks.
/// Optional Blob locks supply lease conditions and ownership-loss cancellation.
/// A supplied lock must identify this exact object and container.
/// Treats only BlobNotFound responses as an absent object.
/// Missing containers, authorization failures and lease errors propagate.
/// Retains its container client and creates an object client for each operation.
/// </summary>
internal sealed class BlobStorageProcessor : IStorageProcessor
{
	private readonly BlobContainerClient _blobContainerClient;

	public BlobStorageProcessor(BlobContainerClient blobContainerClient)
	{
		_blobContainerClient = blobContainerClient;
	}

	public async Task Read(string objectName, Stream destination, IStorageLock? storageLock = null,
		CancellationToken cancellationToken = default)
	{
		ValidateStream(destination, true);
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		BlobClient blob = GetBlob(objectName, cancellation.Token);
		BlobDownloadOptions options = new() { Conditions = GetConditions(blob, objectName, storageLock) };
		Response<BlobDownloadStreamingResult> response = await blob.DownloadStreamingAsync(options, cancellation.Token);
		await using Stream source = response.Value.Content;
		await source.CopyToAsync(destination, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public async Task Write(string objectName, Stream source, bool overwrite, IStorageLock? storageLock = null,
		CancellationToken cancellationToken = default)
	{
		ValidateStream(source, false);
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		BlobClient blob = GetBlob(objectName, cancellation.Token);
		BlobRequestConditions conditions = GetConditions(blob, objectName, storageLock);
		conditions.IfNoneMatch = overwrite ? null : ETag.All;
		await blob.UploadAsync(source, new BlobUploadOptions { Conditions = conditions }, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public async Task Delete(string objectName, IStorageLock? storageLock = null, CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		BlobClient blob = GetBlob(objectName, cancellation.Token);
		BlobRequestConditions conditions = GetConditions(blob, objectName, storageLock);
		await DeleteBlob(blob, conditions, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public async Task<bool> Exists(string objectName, IStorageLock? storageLock = null, CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		BlobClient blob = GetBlob(objectName, cancellation.Token);
		BlobRequestConditions conditions = GetConditions(blob, objectName, storageLock);
		bool exists = await BlobExists(blob, conditions, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
		return exists;
	}

	private static BlobRequestConditions GetConditions(BlobClient blob, string objectName, IStorageLock? storageLock)
	{
		if (storageLock == null)
		{
			return new BlobRequestConditions();
		}

		if (storageLock is not BlobStorageLock ownership || ownership.ObjectName != objectName ||
			ownership.BlobUri != blob.Uri || string.IsNullOrWhiteSpace(ownership.LeaseId))
		{
			throw new ArgumentException("The storage lock does not own the requested blob.", nameof(storageLock));
		}

		return new BlobRequestConditions { LeaseId = ownership.LeaseId };
	}

	private static async Task DeleteBlob(BlobClient blob, BlobRequestConditions conditions, CancellationToken cancellationToken)
	{
		try
		{
			await blob.DeleteAsync(DeleteSnapshotsOption.None, conditions, cancellationToken);
		}
		catch (RequestFailedException exception) when (IsMissing(exception))
		{
		}
	}

	private static async Task<bool> BlobExists(BlobClient blob, BlobRequestConditions conditions, CancellationToken cancellationToken)
	{
		try
		{
			await blob.GetPropertiesAsync(conditions, cancellationToken);
			return true;
		}
		catch (RequestFailedException exception) when (IsMissing(exception))
		{
			return false;
		}
	}

	private BlobClient GetBlob(string objectName, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
		return _blobContainerClient.GetBlobClient(objectName);
	}

	private static CancellationTokenSource LinkCancellation(IStorageLock? storageLock, CancellationToken cancellationToken)
	{
		return CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, storageLock?.CancellationToken ?? CancellationToken.None);
	}

	private static void ValidateStream(Stream stream, bool destination)
	{
		ArgumentNullException.ThrowIfNull(stream);
		if (destination ? !stream.CanWrite : !stream.CanRead)
		{
			throw new ArgumentException(destination ? "The destination must be writable." : "The source must be readable.", nameof(stream));
		}
	}

	private static bool IsMissing(RequestFailedException exception) =>
		exception.Status == 404 && exception.ErrorCode == BlobErrorCode.BlobNotFound;
}
