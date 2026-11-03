using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Contracts.Storage;
using Shared.Impl;

namespace UnitTests.Shared;

/// <summary>
/// Verifies Local storage through its production DI registration.
/// Exercises real file streams, missing objects, errors and cancellation.
/// Uses isolated temporary directories for each test.
/// Replaces the file service only when deterministic error injection is required.
/// </summary>
[TestFixture(Category = "Local")]
public class LocalStorageProcessorTests
{
	private string _root = null!;
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IStorageProcessor _storage = null!;

	[SetUp]
	public void SetUp()
	{
		_root = Directory.CreateTempSubdirectory("shared-storage-tests-").FullName;
		_provider = CreateProvider();
		_scope = _provider.CreateScope();
		_storage = _scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
		Directory.Delete(_root, true);
	}

	[Test]
	public async Task RoundTrip_PreservesCallerStreamsAndUsesTheirCurrentPositions()
	{
		using MemoryStream source = new MemoryStream([9, 1, 2, 3]);
		source.Position = 1;
		using MemoryStream destination = new MemoryStream();
		destination.WriteByte(8);
		await _storage.Write("data.bin", source, false);
		await _storage.Read("data.bin", destination);
		Assert.Multiple(() =>
		{
			Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 8, 1, 2, 3 }));
			Assert.That(source.CanRead, Is.True);
			Assert.That(destination.CanWrite, Is.True);
		});
		Assert.That(await _storage.Exists("data.bin"), Is.True);
	}

	[Test]
	public async Task Read_AcceptsFileStreamAndLeavesItOpen()
	{
		using MemoryStream source = new MemoryStream([1, 2, 3]);
		await _storage.Write("data.bin", source, false);
		await using FileStream destination = File.Create(Path.Combine(_root, "copy.bin"));
		await _storage.Read("data.bin", destination);
		Assert.That(destination.Length, Is.EqualTo(3));
		Assert.That(destination.CanWrite, Is.True);
	}

	[Test]
	public async Task Write_RejectsOverwriteWithoutChangingOriginal_AndTruncatesWhenAllowed()
	{
		await File.WriteAllBytesAsync(Path.Combine(_root, "data.bin"), [1, 2, 3]);
		using MemoryStream source = new MemoryStream([4]);
		Assert.ThrowsAsync<IOException>(() => _storage.Write("data.bin", source, false));
		Assert.That(await File.ReadAllBytesAsync(Path.Combine(_root, "data.bin")),
			Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(source.Position, Is.Zero);
		await _storage.Write("data.bin", source, true);
		Assert.That(await File.ReadAllBytesAsync(Path.Combine(_root, "data.bin")),
			Is.EqualTo(new byte[] { 4 }));
	}

	[TestCase("missing.bin")]
	[TestCase("missing-directory/missing.bin")]
	public async Task MissingObject_ExistsReturnsFalse_AndDeleteSucceeds(string name)
	{
		Assert.That(await _storage.Exists(name), Is.False);
		await _storage.Delete(name);
	}

	[Test]
	public async Task Delete_RemovesObject_AndCanBeRepeated()
	{
		await File.WriteAllBytesAsync(Path.Combine(_root, "data.bin"), [1]);
		await _storage.Delete("data.bin");
		await _storage.Delete("data.bin");
		Assert.That(await _storage.Exists("data.bin"), Is.False);
	}

	[Test]
	public void MissingRead_Throws_AndKeepsDestinationOpen()
	{
		using MemoryStream destination = new MemoryStream();
		Assert.ThrowsAsync<FileNotFoundException>(() => _storage.Read("missing.bin", destination));
		Assert.That(destination.CanWrite, Is.True);
	}

	[Test]
	public void MissingRoot_IsAnErrorForExistsAndDelete()
	{
		using ServiceProvider provider = CreateProvider(root: Path.Combine(_root, "missing-root"));
		using IServiceScope scope = provider.CreateScope();
		IStorageProcessor storage = scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
		Assert.CatchAsync<IOException>(async () => await storage.Exists("data.bin"));
		Assert.CatchAsync<IOException>(() => storage.Delete("data.bin"));
	}
	[TestCase("")]
	[TestCase("..")]
	[TestCase(".")]
	[TestCase("../outside.bin")]
	public void InvalidName_IsRejected(string name)
	{
		Assert.ThrowsAsync<ArgumentException>(async () => await _storage.Exists(name));
	}

	[Test]
	public void AbsoluteName_IsRejected()
	{
		Assert.ThrowsAsync<ArgumentException>(async () =>
			await _storage.Exists(Path.Combine(_root, "data.bin")));
	}

	[Test]
	public void Directory_IsNotAnObject()
	{
		Directory.CreateDirectory(Path.Combine(_root, "folder"));
		Assert.ThrowsAsync<IOException>(async () => await _storage.Exists("folder"));
	}

	[TestCase(true)]
	[TestCase(false)]
	public void Exists_PropagatesAccessAndIoErrors(bool accessDenied)
	{
		IFileService? files = Substitute.For<IFileService>();
		files.GetAttributes(_root).Returns(FileAttributes.Directory);
		Exception error = accessDenied ? new UnauthorizedAccessException() : new IOException();
		files.GetAttributes(Path.Combine(_root, "data.bin")).Returns(_ => throw error);
		using ServiceProvider provider = CreateProvider(files);
		using IServiceScope scope = provider.CreateScope();
		IStorageProcessor storage = scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
		Assert.That(Assert.CatchAsync(async () => await storage.Exists("data.bin")), Is.SameAs(error));
	}

	[Test]
	public void PreCancelledOperations_DoNotReadWriteOrDelete()
	{
		File.WriteAllBytes(Path.Combine(_root, "data.bin"), [1, 2]);
		using MemoryStream stream = new MemoryStream([3]);
		CancellationToken token = new CancellationToken(true);
		Assert.ThrowsAsync<OperationCanceledException>(() => _storage.Read("data.bin", stream, null, token));
		Assert.ThrowsAsync<OperationCanceledException>(() => _storage.Write("data.bin", stream, true, null, token));
		Assert.ThrowsAsync<OperationCanceledException>(() => _storage.Delete("data.bin", null, token));
		Assert.ThrowsAsync<OperationCanceledException>(async () => await _storage.Exists("data.bin", null, token));
		Assert.That(File.ReadAllBytes(Path.Combine(_root, "data.bin")), Is.EqualTo(new byte[] { 1, 2 }));
	}

	[Test]
	public async Task Factory_ReadsCurrentConfiguration_AndExistingProcessorsKeepTheirRoot()
	{
		IConfiguration configuration = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
		IStorageProcessorFactory factory = _scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>();
		string secondRoot = Directory.CreateDirectory(Path.Combine(_root, "second")).FullName;
		configuration["fileStorage:Root"] = secondRoot;
		IStorageProcessor second = factory.CreateProcessor();
		using MemoryStream firstSource = new([1]);
		using MemoryStream secondSource = new([2]);
		await Task.WhenAll(_storage.Write("data.bin", firstSource, false), second.Write("data.bin", secondSource, false));
		Assert.That(await File.ReadAllBytesAsync(Path.Combine(_root, "data.bin")), Is.EqualTo(new byte[] { 1 }));
		Assert.That(await File.ReadAllBytesAsync(Path.Combine(secondRoot, "data.bin")), Is.EqualTo(new byte[] { 2 }));
	}

	[TestCase(null)]
	[TestCase("")]
	[TestCase(" ")]
	public void Factory_RejectsMissingOrBlankRoot(string? root)
	{
		IConfiguration configuration = _scope.ServiceProvider.GetRequiredService<IConfiguration>();
		configuration["fileStorage:Root"] = root;
		IStorageProcessorFactory factory = _scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>();
		Assert.Throws<InvalidOperationException>(() => factory.CreateProcessor());
	}

	[Test]
	public async Task Factory_ResolvesRelativeRootAgainstApplicationDirectory()
	{
		using ServiceProvider provider = CreateProvider(root: ".");
		using IServiceScope scope = provider.CreateScope();
		IStorageProcessor storage = scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>().CreateProcessor();
		Assert.That(await storage.Exists(Path.GetFileName(typeof(LocalStorageProcessorTests).Assembly.Location)), Is.True);
	}

	[Test]
	public void Factory_CreatesFreshProcessors_WithoutDirectProcessorRegistration()
	{
		IStorageProcessorFactory factory = _scope.ServiceProvider.GetRequiredService<IStorageProcessorFactory>();
		Assert.That(factory.CreateProcessor(), Is.Not.SameAs(factory.CreateProcessor()));
		Assert.That(_scope.ServiceProvider.GetService<IStorageProcessor>(), Is.Null);
	}

	[Test]
	public void NullObjectName_IsRejected()
	{
		Assert.ThrowsAsync<ArgumentNullException>(async () => await _storage.Exists(null!));
	}

	[Test]
	public async Task LockedRoundTrip_DoesNotReleaseCallerLock()
	{
		IStorageLockFactory factory = _scope.ServiceProvider.GetRequiredService<IStorageLockFactory>();
		await using IStorageLock ownership = (await factory.TryAcquire("data.bin"))!;
		using MemoryStream source = new([1, 2, 3]);
		using MemoryStream destination = new();
		await _storage.Write("data.bin", source, false, ownership);
		Assert.That(await _storage.Exists("data.bin", ownership), Is.True);
		await _storage.Read("data.bin", destination, ownership);
		await _storage.Delete("data.bin", ownership);
		Assert.That(destination.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(await _storage.Exists("data.bin", ownership), Is.False);
		await using IStorageLock? competing = await factory.TryAcquire("data.bin");
		Assert.That(competing, Is.Null);
	}

	[Test]
	public void CancelledOwnership_PreventsAllOperations()
	{
		File.WriteAllBytes(Path.Combine(_root, "data.bin"), [1, 2]);
		IStorageLock ownership = Substitute.For<IStorageLock>();
		ownership.CancellationToken.Returns(new CancellationToken(true));
		using MemoryStream stream = new([3]);
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Read("data.bin", stream, ownership));
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Write("data.bin", stream, true, ownership));
		Assert.CatchAsync<OperationCanceledException>(() => _storage.Delete("data.bin", ownership));
		Assert.CatchAsync<OperationCanceledException>(async () => await _storage.Exists("data.bin", ownership));
		Assert.That(File.ReadAllBytes(Path.Combine(_root, "data.bin")), Is.EqualTo(new byte[] { 1, 2 }));
	}

	[Test]
	public async Task InFlightTransfer_ObservesCancellation(
		[Values(false, true)] bool read,
		[Values(false, true)] bool cancelOwnership)
	{
		await File.WriteAllBytesAsync(Path.Combine(_root, "data.bin"), [1, 2]);
		using CancellationTokenSource ownershipCancellation = new();
		using CancellationTokenSource caller = new();
		IStorageLock ownership = Substitute.For<IStorageLock>();
		ownership.CancellationToken.Returns(ownershipCancellation.Token);
		using BlockingStorageStream stream = new();
		Task pending = read
			? _storage.Read("data.bin", stream, ownership, caller.Token)
			: _storage.Write("data.bin", stream, true, ownership, caller.Token);
		await stream.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await (cancelOwnership ? ownershipCancellation : caller).CancelAsync();
		Assert.CatchAsync<OperationCanceledException>(() => pending.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.That(stream.CanWrite, Is.True);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedCopy_PreservesDestinationAndRemovesCandidate(bool existing)
	{
		string path = Path.Combine(_root, "data.bin");
		if (existing)
		{
			await File.WriteAllBytesAsync(path, [1, 2, 3]);
		}
		using PausingStorageStream source = new([4, 5, 6]);
		IOException failure = new("Source transfer failed.");
		source.Failure = failure;
		Task write = _storage.Write("data.bin", source, true);
		await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		source.Release.TrySetResult();
		Assert.That(Assert.ThrowsAsync<IOException>(() => write), Is.SameAs(failure));
		Assert.That(source.CanRead, Is.True);
		if (existing)
		{
			Assert.That(await File.ReadAllBytesAsync(path), Is.EqualTo(new byte[] { 1, 2, 3 }));
		}
		else
		{
			Assert.That(File.Exists(path), Is.False);
		}
		Assert.That(Directory.GetFiles(_root, "data_*.bin"), Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ConcurrentWrites_UseUniqueCandidatesAndRespectOverwrite(bool overwrite)
	{
		string directory = Directory.CreateDirectory(Path.Combine(_root, "nested")).FullName;
		using PausingStorageStream first = new([1, 2, 3]);
		using PausingStorageStream second = new([4, 5, 6]);
		Task firstWrite = _storage.Write("nested/data.bin", first, overwrite);
		await first.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Task secondWrite = _storage.Write("nested/data.bin", second, overwrite);
		await second.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		try
		{
			string[] candidates = Directory.GetFiles(directory, "data_*.bin");
			Assert.That(candidates, Has.Length.EqualTo(2));
			foreach (string candidate in candidates)
			{
				Assert.That(Path.GetFileName(candidate), Does.Match(@"^data_[0-9a-f]{32}\.bin$"));
			}
			Assert.That(File.Exists(Path.Combine(directory, "data.bin")), Is.False);
		}
		finally
		{
			first.Release.TrySetResult();
			await firstWrite;
			second.Release.TrySetResult();
			if (overwrite)
			{
				await secondWrite;
			}
			else
			{
				Assert.ThrowsAsync<IOException>(() => secondWrite);
			}
		}
		Assert.That(await File.ReadAllBytesAsync(Path.Combine(directory, "data.bin")),
			Is.EqualTo(overwrite ? new byte[] { 4, 5, 6 } : new byte[] { 1, 2, 3 }));
		Assert.That(Directory.GetFiles(directory), Has.Length.EqualTo(1));
	}

	[Test]
	public async Task Replacement_AllowsExistingReadToFinishWithOriginalContents()
	{
		await File.WriteAllBytesAsync(Path.Combine(_root, "data.bin"), [1, 2, 3]);
		using PausingStorageStream reader = new([]);
		Task read = _storage.Read("data.bin", reader);
		await reader.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		try
		{
			using MemoryStream source = new([4, 5, 6]);
			await _storage.Write("data.bin", source, true);
			using MemoryStream current = new();
			await _storage.Read("data.bin", current);
			Assert.That(current.ToArray(), Is.EqualTo(new byte[] { 4, 5, 6 }));
		}
		finally
		{
			reader.Release.TrySetResult();
			await read;
		}
		Assert.That(reader.ToArray(), Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task CancelledPartialCopy_PreservesOriginalAndRemovesCandidate(bool ownershipLost)
	{
		await File.WriteAllBytesAsync(Path.Combine(_root, "data.bin"), [1, 2, 3]);
		using CancellationTokenSource cancellation = new();
		IStorageLock ownership = Substitute.For<IStorageLock>();
		ownership.CancellationToken.Returns(ownershipLost ? cancellation.Token : CancellationToken.None);
		using PausingStorageStream source = new([4, 5, 6]);
		Task write = _storage.Write(
			"data.bin",
			source,
			true,
			ownership,
			ownershipLost ? CancellationToken.None : cancellation.Token);
		await source.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await cancellation.CancelAsync();
		Assert.CatchAsync<OperationCanceledException>(() => write.WaitAsync(TimeSpan.FromSeconds(5)));
		Assert.That(await File.ReadAllBytesAsync(Path.Combine(_root, "data.bin")), Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(Directory.GetFiles(_root), Has.Length.EqualTo(1));
	}

	private ServiceProvider CreateProvider(IFileService? fileService = null, string? root = null)
	{
		ServiceCollection services = new ServiceCollection();
		services.AddSharedModule();
		services.AddSingleton<IConfiguration>(_ => CreateConfiguration(root ?? _root));
		if (fileService != null)
		{
			services.AddScoped<IFileService>(_ => fileService);
		}

		return services.BuildServiceProvider(new ServiceProviderOptions
		{
			ValidateScopes = true,
			ValidateOnBuild = true
		});
	}

	private static IConfigurationRoot CreateConfiguration(string root)
	{
		string json = JsonSerializer.Serialize(new { fileStorage = new { Root = root } });
		using MemoryStream stream = new(Encoding.UTF8.GetBytes(json));
		return new ConfigurationBuilder().AddJsonStream(stream).Build();
	}
}
