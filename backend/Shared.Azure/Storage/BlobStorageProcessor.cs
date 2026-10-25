using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Shared.Contracts.Storage;

namespace Shared.Azure.Storage;

/// <summary>
/// Stores objects in the Azure Blob container supplied by its factory.
/// Streams content without taking ownership of caller-provided streams.
/// Treats only BlobNotFound responses as an absent object.
/// Missing containers, authorization failures and service errors propagate.
/// Retains its container client and creates an object client for each operation.
/// </summary>
internal sealed class BlobStorageProcessor : IStorageProcessor
{
	private readonly BlobContainerClient _blobContainerClient;

	public BlobStorageProcessor(BlobContainerClient blobContainerClient)
	{
		_blobContainerClient = blobContainerClient;
	}

	public async Task Read(string objectName, Stream destination, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(destination);
		if (!destination.CanWrite)
		{
			throw new ArgumentException("The destination must be writable.", nameof(destination));
		}

		BlobClient blob = GetBlob(objectName, cancellationToken);
		Response<BlobDownloadStreamingResult> response = await blob.DownloadStreamingAsync(new BlobDownloadOptions(), cancellationToken);
		await using Stream source = response.Value.Content;
		await source.CopyToAsync(destination, cancellationToken);
	}

	public async Task Write(string objectName, Stream source, bool overwrite, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		if (!source.CanRead)
		{
			throw new ArgumentException("The source must be readable.", nameof(source));
		}

		BlobClient blob = GetBlob(objectName, cancellationToken);
		await blob.UploadAsync(source, overwrite, cancellationToken);
	}

	public async Task Delete(string objectName, CancellationToken cancellationToken = default)
	{
		BlobClient blob = GetBlob(objectName, cancellationToken);
		try
		{
			await blob.DeleteAsync(cancellationToken: cancellationToken);
		}
		catch (RequestFailedException exception) when (IsMissing(exception))
		{
		}
	}

	public async Task<bool> Exists(string objectName, CancellationToken cancellationToken = default)
	{
		BlobClient blob = GetBlob(objectName, cancellationToken);
		try
		{
			await blob.GetPropertiesAsync(cancellationToken: cancellationToken);
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

	private static bool IsMissing(RequestFailedException exception) => exception.Status == 404 && exception.ErrorCode == BlobErrorCode.BlobNotFound;
}
