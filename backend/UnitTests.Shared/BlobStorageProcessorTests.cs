using Azure.Storage.Blobs.Specialized;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;
using Shared.Azure.Storage;
using Shared.Contracts;
using Azure;
using Azure.Core.Pipeline;
using Azure.Storage.Blobs;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared.Azure;
using Shared.Contracts.Storage;
using Shared.Impl;

namespace UnitTests.Shared;

/// <summary>
/// Verifies configured Azure processors through an in-memory HTTP transport.
/// Overrides the factory registration with TestBlobStorageProcessorFactory.
/// Exercises real SDK request construction without a live Azure account.
/// Covers streams, overwrite conditions, storage failures and cancellation.
/// Each fixture owns the transport; processors retain the container selected by their factory.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class BlobStorageProcessorTests : IDisposable
{
	private BlobStorageTestHandler _handler = null!;
	private HttpClient _httpClient = null!;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IStorageProcessor _storage = null!;

	[SetUp]
	public void SetUp()
	{
		_handler = new BlobStorageTestHandler();
		_httpClient = new HttpClient(_handler);
		_provider = CreateProvider();
		_scope = _provider.CreateScope();
		_storage = _scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
	}

	[TearDown]
	public void Dispose()
	{
		_scope.Dispose();
		_provider.Dispose();
		_httpClient.Dispose();
		_handler.Dispose();
		GC.SuppressFinalize(this);
	}

	[Test]
	public async Task Read_CopiesContentAtCurrentPosition_AndKeepsDestinationOpen()
	{
		using MemoryStream destination = new MemoryStream();
		destination.WriteByte(8);
		await _storage.Read("data.bin", destination);
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 8, 1, 2, 3 }));
		Assert.That(destination.CanWrite, Is.True);
		Assert.That(_handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Get));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Write_UsesSourcePositionAndOverwrite_AndKeepsSourceOpen(bool overwrite)
	{
		using MemoryStream source = new MemoryStream([9, 1, 2]);
		source.Position = 1;
		await _storage.Write("data.bin", source, overwrite);
		(Uri Uri, HttpMethod Method, byte[] Content, string? IfNoneMatch, string? Authorization, string? LeaseId) request = _handler.Requests.Single();
		Assert.That(request.Method, Is.EqualTo(HttpMethod.Put));
		Assert.That(request.Content, Is.EqualTo(new byte[] { 1, 2 }));
		Assert.That(request.IfNoneMatch, Is.EqualTo(overwrite ? null : "*"));
		Assert.That(source.CanRead, Is.True);
	}

	[Test]
	public void Write_PropagatesConflict_AndKeepsSourceOpen()
	{
		_handler.ResponseFactory = _ => BlobStorageTestHandler.Failure(409, "BlobAlreadyExists");
		using MemoryStream source = new MemoryStream([1]);
		RequestFailedException? error = Assert.ThrowsAsync<RequestFailedException>(() => _storage.Write("data.bin", source, false));
		Assert.That(error!.ErrorCode, Is.EqualTo("BlobAlreadyExists"));
		Assert.That(source.CanRead, Is.True);
	}

	[Test]
	public async Task Exists_ReturnsTrueForAnExistingObject()
	{
		Assert.That(await _storage.Exists("data.bin"), Is.True);
		Assert.That(_handler.Requests.Single().Method, Is.EqualTo(HttpMethod.Head));
	}

	[Test]
	public async Task MissingBlob_ExistsReturnsFalse_AndDeleteSucceeds()
	{
		_handler.ResponseFactory = _ => BlobStorageTestHandler.Failure(404, "BlobNotFound");
		Assert.That(await _storage.Exists("data.bin"), Is.False);
		await _storage.Delete("data.bin");
		Assert.That(_handler.Requests.Last().Method, Is.EqualTo(HttpMethod.Delete));
	}

	[TestCase(404, "ContainerNotFound")]
	[TestCase(404, null)]
	[TestCase(403, "AuthorizationFailure")]
	[TestCase(503, "ServerBusy")]
	public void ExistsAndDelete_PropagateStorageFailures(int status, string? code)
	{
		_handler.ResponseFactory = _ => BlobStorageTestHandler.Failure(status, code);
		RequestFailedException? existsError = Assert.ThrowsAsync<RequestFailedException>(async () =>
			await _storage.Exists("data.bin"));
		RequestFailedException? deleteError = Assert.ThrowsAsync<RequestFailedException>(() =>
			_storage.Delete("data.bin"));
		Assert.That(existsError!.Status, Is.EqualTo(status));
		Assert.That(deleteError!.Status, Is.EqualTo(status));
	}

	[Test]
	public void Read_PropagatesMissingObject()
	{
		_handler.ResponseFactory = _ => BlobStorageTestHandler.Failure(404, "BlobNotFound");
		using MemoryStream destination = new MemoryStream();
		RequestFailedException? error = Assert.ThrowsAsync<RequestFailedException>(() => _storage.Read("data.bin", destination));
		Assert.That(error!.ErrorCode, Is.EqualTo("BlobNotFound"));
		Assert.That(destination.CanWrite, Is.True);
	}

	[Test]
	public void PreCancelledOperations_DoNotContactAzure()
	{
		using MemoryStream stream = new MemoryStream([1]);
		CancellationToken token = new CancellationToken(true);
		Assert.ThrowsAsync<OperationCanceledException>(() => _storage.Read("data.bin", stream, null, token));
		Assert.ThrowsAsync<OperationCanceledException>(() => _storage.Write("data.bin", stream, true, null, token));
		Assert.ThrowsAsync<OperationCanceledException>(() => _storage.Delete("data.bin", null, token));
		Assert.ThrowsAsync<OperationCanceledException>(async () => await _storage.Exists("data.bin", null, token));
		Assert.That(_handler.Requests, Is.Empty);
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void InvalidObjectName_IsRejectedByAllOperations(string? name)
	{
		using MemoryStream stream = new([1]);
		Assert.CatchAsync<ArgumentException>(() => _storage.Read(name!, stream));
		Assert.CatchAsync<ArgumentException>(() => _storage.Write(name!, stream, false));
		Assert.CatchAsync<ArgumentException>(() => _storage.Delete(name!));
		Assert.CatchAsync<ArgumentException>(async () => await _storage.Exists(name!));
		Assert.That(_handler.Requests, Is.Empty);
	}


	[TestCase("Read")]
	[TestCase("Write")]
	[TestCase("Delete")]
	[TestCase("Exists")]
	public async Task LockedOperation_SendsLeaseId_AndLeavesLockOwned(string operation)
	{
		await using BlobStorageLock ownership = CreateLock();
		await RunOperation(operation, ownership);
		Assert.That(_handler.Requests.Single().LeaseId, Is.EqualTo(ownership.LeaseId));
		Assert.That(ownership.CancellationToken.IsCancellationRequested, Is.False);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task LockedWrite_PreservesOverwriteCondition(bool overwrite)
	{
		await using BlobStorageLock ownership = CreateLock();
		using MemoryStream source = new([1]);
		await _storage.Write("data.bin", source, overwrite, ownership);
		Assert.That(_handler.Requests.Single().IfNoneMatch, Is.EqualTo(overwrite ? null : "*"));
	}

	[TestCase("Read")]
	[TestCase("Write")]
	[TestCase("Delete")]
	[TestCase("Exists")]
	public async Task InvalidLock_IsRejectedBeforeRequest(string operation)
	{
		await using BlobStorageLock wrongObject = CreateLock("other.bin");
		await using BlobStorageLock wrongContainer = CreateLock(uri: "https://account.blob.core.windows.net/other/data.bin");
		Assert.ThrowsAsync<ArgumentException>(() => RunOperation(operation, wrongObject));
		Assert.ThrowsAsync<ArgumentException>(() => RunOperation(operation, wrongContainer));
		Assert.ThrowsAsync<ArgumentException>(() => RunOperation(operation, Substitute.For<IStorageLock>()));
		Assert.That(_handler.Requests, Is.Empty);
	}

	[TestCase("Read")]
	[TestCase("Write")]
	[TestCase("Delete")]
	[TestCase("Exists")]
	public async Task DisposedLock_PreventsRequest(string operation)
	{
		BlobStorageLock ownership = CreateLock();
		await ownership.DisposeAsync();
		Assert.CatchAsync<OperationCanceledException>(() => RunOperation(operation, ownership));
		Assert.That(_handler.Requests, Is.Empty);
	}

	[TestCase("Read")]
	[TestCase("Write")]
	[TestCase("Delete")]
	[TestCase("Exists")]
	public async Task LeaseFailure_Propagates(string operation)
	{
		await using BlobStorageLock ownership = CreateLock();
		_handler.ResponseFactory = _ => BlobStorageTestHandler.Failure(412, "LeaseIdMismatchWithBlobOperation");
		RequestFailedException? error = Assert.ThrowsAsync<RequestFailedException>(() => RunOperation(operation, ownership));
		Assert.That(error!.ErrorCode, Is.EqualTo("LeaseIdMismatchWithBlobOperation"));
	}

	[Test]
	public async Task InFlightOperation_ObservesCancellation(
		[Values("Read", "Write", "Delete", "Exists")] string operation,
		[Values(false, true)] bool cancelOwnership)
	{
		await using BlobStorageLock ownership = CreateLock();
		using CancellationTokenSource caller = new();
		TaskCompletionSource entered = BlockRequests();
		Task pending = RunOperation(operation, ownership, caller.Token);
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		if (cancelOwnership)
		{
			await ownership.DisposeAsync();
		}
		else
		{
			await caller.CancelAsync();
		}

		Assert.CatchAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
	}

	private TaskCompletionSource BlockRequests()
	{
		TaskCompletionSource entered = new(TaskCreationOptions.RunContinuationsAsynchronously);
		_handler.AsyncResponseFactory = async (request, token) =>
		{
			entered.TrySetResult();
			await Task.Delay(Timeout.Infinite, token);
			return BlobStorageTestHandler.Success(request);
		};
		return entered;
	}

	private BlobStorageLock CreateLock(string objectName = "data.bin", string? uri = null)
	{
		BlobLeaseClient lease = Substitute.For<BlobLeaseClient>();
		lease.Uri.Returns(new Uri(uri ?? $"https://account.blob.core.windows.net/data/{objectName}"));
		lease.LeaseId.Returns("7bf52858-4723-4e54-ad38-7fd9df9c660a");
		FakeTimeProvider clock = new();
		ISharedContext context = _scope.ServiceProvider.GetRequiredService<ISharedContext>();
		return new BlobStorageLock(objectName, lease, clock, context, clock.GetTimestamp());
	}

	private async Task RunOperation(string operation, IStorageLock ownership, CancellationToken cancellationToken = default)
	{
		using MemoryStream stream = new();
		stream.WriteByte(1);
		stream.Position = 0;
		await (operation switch
		{
			"Read" => _storage.Read("data.bin", stream, ownership, cancellationToken),
			"Write" => _storage.Write("data.bin", stream, true, ownership, cancellationToken),
			"Delete" => _storage.Delete("data.bin", ownership, cancellationToken),
			"Exists" => _storage.Exists("data.bin", ownership, cancellationToken),
			_ => throw new ArgumentOutOfRangeException(nameof(operation))
		});
	}

	private BlobContainerClient CreateContainerClient()
	{
		BlobClientOptions options = new() { Transport = new HttpClientTransport(_httpClient) };
		options.Retry.MaxRetries = 0;
		return new BlobContainerClient(
			new Uri("https://account.blob.core.windows.net/data"),
			new BlobStorageTestCredential("test-token"), options);
	}

	private ServiceProvider CreateProvider()
	{
		ServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddSharedAzureModule();
		services.AddScoped<IStorageProcessorFactory>(_ =>
			new TestBlobStorageProcessorFactory(CreateContainerClient()));
		return services.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateScopes = true,
			ValidateOnBuild = true
		});
	}
}
