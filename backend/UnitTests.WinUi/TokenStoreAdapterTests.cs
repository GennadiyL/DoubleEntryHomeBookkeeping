using System.Text;
using Adapter.TokenStore.WinUi;
using NUnit.Framework;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Impl;
using Synchronization.Contracts.Adapters.TokenStore;

namespace UnitTests.WinUi;

/// <summary>
/// Verifies the Windows token store using real current-user DPAPI protection.
/// Every test uses a unique temporary directory and synthetic token values.
/// Round trips check replacement and deletion without involving the Master server.
/// Scope checks isolate server addresses, datasets and Local registrations.
/// Copied ciphertext must not decrypt under a different scope.
/// Corrupt files are treated as missing authorization rather than usable tokens.
/// Cancellation must leave the previously persisted token available.
/// The tests require Windows and perform no network calls or real credential access.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TokenStoreAdapterTests
{
	private string _directory = null!;
	private TokenStoreAdapter _store = null!;
	private TokenStoreScope _scope = null!;
	private ServiceProvider _provider = null!;
	private IServiceScope _services = null!;

	[SetUp]
	public void SetUp()
	{
		_directory = Path.Combine(Path.GetTempPath(), "Dehb.TokenStore.Tests", Guid.NewGuid().ToString("N"));
		ServiceCollection services = new();
		services.AddSharedModule();
		_provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		_services = _provider.CreateScope();
		_store = new TokenStoreAdapter(_services.ServiceProvider.GetRequiredService<ISharedContext>(), _directory);
		_scope = new TokenStoreScope
		{
			MasterAddress = new Uri("https://master.example.test/api/"),
			MasterDatasetKey = "master-one",
			LocalDatasetKey = "local-one"
		};
	}

	[TearDown]
	public void TearDown()
	{
		_services?.Dispose();
		_provider?.Dispose();
		if (Directory.Exists(_directory))
		{
			Directory.Delete(_directory, true);
		}
	}

	[Test]
	public async Task Save_ProtectsAndReplacesToken_DeleteIsIdempotent()
	{
		Assert.That(await _store.Read(_scope), Is.Null);
		await _store.Delete(_scope);
		const string token = "synthetic-token-for-dpapi-round-trip";
		await _store.Save(_scope, token);
		Assert.That(await _store.Read(_scope), Is.EqualTo(token));
		byte[] persisted = await File.ReadAllBytesAsync(Directory.GetFiles(_directory, "*.bin").Single());
		Assert.That(Encoding.UTF8.GetString(persisted), Does.Not.Contain(token));
		await _store.Save(_scope, "replacement-token");
		Assert.That(await _store.Read(_scope), Is.EqualTo("replacement-token"));
		Assert.That(Directory.GetFiles(_directory), Has.Length.EqualTo(1));
		await _store.Delete(_scope);
		await _store.Delete(_scope);
		Assert.That(await _store.Read(_scope), Is.Null);
	}

	[TestCase("address")]
	[TestCase("dataset")]
	[TestCase("registration")]
	public async Task Read_RejectsCiphertextCopiedFromAnotherScope(string component)
	{
		await _store.Save(_scope, "original-token");
		string originalPath = Directory.GetFiles(_directory, "*.bin").Single();
		TokenStoreScope other = component switch
		{
			"address" => _scope with { MasterAddress = new Uri("https://other.example.test/api/") },
			"dataset" => _scope with { MasterDatasetKey = "master-two" },
			_ => _scope with { LocalDatasetKey = "local-two" }
		};
		Assert.That(await _store.Read(other), Is.Null);
		await _store.Save(other, "other-token");
		string otherPath = Directory.GetFiles(_directory, "*.bin").Single(path => path != originalPath);
		File.Copy(originalPath, otherPath, true);
		Assert.That(await _store.Read(other), Is.Null);
		Assert.That(await _store.Read(_scope), Is.EqualTo("original-token"));
		await _store.Delete(other);
		Assert.That(await _store.Read(_scope), Is.EqualTo("original-token"));
	}

	[Test]
	public async Task Read_CorruptFile_RequiresAuthentication()
	{
		await _store.Save(_scope, "token");
		await File.WriteAllBytesAsync(Directory.GetFiles(_directory, "*.bin").Single(), [1, 2, 3]);
		Assert.That(await _store.Read(_scope), Is.Null);
	}

	[Test]
	public async Task CancelledOperations_LeaveSavedTokenUnchanged()
	{
		await _store.Save(_scope, "original-token");
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		Assert.ThrowsAsync<OperationCanceledException>(async () => await _store.Read(_scope, cancellation.Token));
		Assert.ThrowsAsync<OperationCanceledException>(async () => await _store.Save(_scope, "replacement-token", cancellation.Token));
		Assert.ThrowsAsync<OperationCanceledException>(async () => await _store.Delete(_scope, cancellation.Token));
		Assert.That(await _store.Read(_scope), Is.EqualTo("original-token"));
	}

	[TestCase("http://remote.example.test/")]
	[TestCase("https://user:password@master.example.test/")]
	[TestCase("https://master.example.test/?token=value")]
	[TestCase("https://master.example.test/#fragment")]
	public void Save_RejectsUnsafeAddresses(string address)
	{
		_scope.MasterAddress = new Uri(address);
		Assert.ThrowsAsync<ArgumentException>(async () => await _store.Save(_scope, "token"));
		Assert.That(Directory.Exists(_directory), Is.False);
	}

	[Test]
	public async Task Save_AllowsLocalhostHttpForDevelopment()
	{
		_scope.MasterAddress = new Uri("http://localhost:7071/api/");
		await _store.Save(_scope, "development-token");
		Assert.That(await _store.Read(_scope), Is.EqualTo("development-token"));
	}
}
