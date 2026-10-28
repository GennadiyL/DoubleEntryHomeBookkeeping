using Business.Models.Enums;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.EntityFramework.SqLite;
using Master.Impl;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;
using Synchronization.Contracts.Adapters;
using Synchronization.Contracts.Adapters.Master;
using Synchronization.Contracts.Services;
using Synchronization.Contracts.Services.Synchronizations;
using Synchronization.Impl;
using Synchronization.Impl.Adapters;
using Exchange.Models.Synchronizations;

namespace UnitTests.Synchronization;

/// <summary>
/// Exercises the Local workflow against independent production SQLite databases.
/// Real capture, merge, snapshot preparation and acknowledgement run through DI.
/// A substitute adapter injects failures at individual provider boundaries.
/// Retry tests verify direct container persistence and immutable batch deduplication.
/// Cancellation preserves pending work and never acknowledges uninstalled data.
/// Candidate installation preserves Local settings and the actual installed revision.
/// Invalid snapshots and Local persistence failures leave the original database intact.
/// Every test owns its files and never opens an existing user database.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class SynchronizationWorkflowTests
{
	private DirectoryInfo _directory = null!;
	private ServiceProvider _provider = null!;
	private IMasterAdapter _adapter = null!;
	private LocalMasterAdapter _real = null!;
	private Guid _categoryId;
	private string LocalPath => Path.Combine(_directory.FullName, "local.db");
	private string MasterPath => Path.Combine(_directory.FullName, "master.db");

	[SetUp]
	public async Task SetUp()
	{
		_directory = Directory.CreateTempSubdirectory("dehb-workflow-");
		foreach (string path in new[] { LocalPath, MasterPath })
		{
			await using SqliteConnection connection = new($"Data Source={path};Pooling=False");
			await connection.OpenAsync();
			foreach (string script in new[] { "001-schema.sql", "002-initial-data.sql" })
			{
				await using SqliteCommand command = connection.CreateCommand();
				command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", script));
				await command.ExecuteNonQueryAsync();
			}
		}
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ConnectionStrings:DehbSqLite"] = $"Data Source={LocalPath};Pooling=False",
			["ConnectionStrings:DehbMasterSqLite"] = $"Data Source={MasterPath};Pooling=False",
			["fileStorage:Root"] = _directory.FullName
		}).Build();
		ServiceCollection services = new();
		services.AddSingleton(configuration);
		services.AddSharedModule();
		services.AddLocalDataAccessSqLiteModule(configuration);
		services.AddMasterDataAccessSqLiteModule(configuration);
		services.AddMasterModule();
		services.AddLocalMasterStorage(_ => MasterPath);
		services.AddSynchronizationModule();
		_adapter = Substitute.For<IMasterAdapter>();
		services.AddSingleton(_adapter);
		_provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		_real = new LocalMasterAdapter(_provider.GetRequiredService<IServiceScopeFactory>());
		ResetAdapter();
		using IServiceScope scope = _provider.CreateScope();
		ILocalAppUnitOfWork local = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		IMasterAppUnitOfWork master = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		LocalConfig config = (await local.LocalConfigRepo.GetAll()).Single();
		config.AccountNameSeparator = "|";
		local.LocalConfigRepo.Update(config);
		_categoryId = Guid.NewGuid();
		CategoryGroup root = (await local.CategoryGroupRepo.GetAll()).Single();
		local.CategoryRepo.Add(new Category { Id = _categoryId, GroupId = root.Id, Name = "Local category" });
		await local.SaveChanges();
		master.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = config.LocalDatasetKey });
		await master.SaveChanges();
	}

	[TearDown]
	public async Task TearDown()
	{
		if (_provider is not null) { await _provider.DisposeAsync(); }
		if (_directory is not null) { _directory.Delete(true); }
	}

	[Test]
	public async Task Complete_PreservesLocalSettingsAndAcknowledgesActualDownloadRevision()
	{
		_adapter.Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			using IServiceScope scope = _provider.CreateScope();
			IMasterAppUnitOfWork master = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
			SystemConfig config = (await master.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision++;
			master.SystemConfigRepo.Update(config);
			await master.SaveChanges();
			return await _real.Download(call.Arg<DownloadRequest>(), call.Arg<CancellationToken>());
		});
		SynchronizeInfo result = await Run();
		Assert.That(result.Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		Assert.That(result.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.None));
		Assert.That(result.Revision, Is.EqualTo(2));
		using IServiceScope scope = _provider.CreateScope();
		ILocalAppUnitOfWork local = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
		LocalConfig config = (await local.LocalConfigRepo.GetAll()).Single();
		Assert.That(config.SnapshotRevision, Is.EqualTo(2));
		Assert.That(config.AccountNameSeparator, Is.EqualTo("|"));
		Assert.That((await local.CategoryRepo.GetById(_categoryId))!.EditRevision, Is.EqualTo(1));
		Assert.That((await ReadPending())!.Batches, Is.Empty);
		Assert.That((await ReadPending())!.Revision, Is.EqualTo(config.SnapshotRevision));
		IMasterAppUnitOfWork master = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		Assert.That((await master.LocalRegistrationRepo.GetByLocalDatasetKey(config.LocalDatasetKey))!.SyncRevision, Is.EqualTo(2));
	}

	[Test]
	public async Task LostSubmissionResponse_ReplaysOriginalBatchWithoutDuplicateMerge()
	{
		_adapter.Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>()).Returns<Task<SynchronizeResponse>>(async call =>
		{
			await _real.Synchronize(call.Arg<SynchronizeRequest>(), call.Arg<CancellationToken>());
			throw new MasterRequestException(SynchronizationStopReason.ConnectionLost, "Response lost.");
		});
		SynchronizeInfo stopped = await Run();
		Assert.That(stopped.Outcome, Is.EqualTo(SynchronizationOutcome.Deferred));
		Assert.That(stopped.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.Captured));
		SyncContainer pending = (await ReadPending())!;
		string key = pending.Batches.Single().SyncKey;
		ResetAdapter();
		SynchronizeInfo completed = await Run();
		Assert.That(completed.Revision, Is.EqualTo(1));
		Assert.That(completed.Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		using IServiceScope scope = _provider.CreateScope();
		ICollection<LocalSyncBatch> receipts = await scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>().LocalSyncBatchRepo.GetAll();
		Assert.That(receipts.Single().SyncKey, Is.EqualTo(key));
	}

	[TestCase(SynchronizationStopReason.TokenMissing)]
	[TestCase(SynchronizationStopReason.TokenExpired)]
	[TestCase(SynchronizationStopReason.TokenRejected)]
	public async Task AuthorizationStopDuringDownload_RetainsSubmittedCheckpoint(SynchronizationStopReason reason)
	{
		_adapter.Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<DownloadResponse>(new MasterRequestException(reason, "Authorization required.")));
		SynchronizeInfo stopped = await Run();
		Assert.That(stopped.Outcome, Is.EqualTo(SynchronizationOutcome.AuthorizationRequired));
		Assert.That(stopped.Reason, Is.EqualTo(reason));
		Assert.That(stopped.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.Submitted));
		ResetAdapter();
		_adapter.ClearReceivedCalls();
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		await _adapter.Received(1).Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task RejectedAcknowledgement_RestartsWithPrunedContainer()
	{
		_adapter.Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>()).Returns(new AcknowledgeResponse { IsSuccess = false });
		SynchronizeInfo stopped = await Run();
		Assert.That(stopped.Reason, Is.EqualTo(SynchronizationStopReason.AcknowledgementNotAccepted));
		Assert.That(stopped.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.Installed));
		Assert.That((await ReadPending())!.Revision, Is.EqualTo(1));
		ResetAdapter();
		_adapter.ClearReceivedCalls();
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		await _adapter.Received(1).Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>());
		await _adapter.DidNotReceive().Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task CancellationAfterMasterCommit_RetainsUncertainSubmissionForRetry()
	{
		using CancellationTokenSource cancellation = new();
		_adapter.Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>()).Returns<Task<SynchronizeResponse>>(async call =>
		{
			await _real.Synchronize(call.Arg<SynchronizeRequest>(), call.Arg<CancellationToken>());
			cancellation.Cancel();
			throw new OperationCanceledException(cancellation.Token);
		});
		SynchronizeInfo result = await Run(cancellation.Token);
		Assert.That(result.Outcome, Is.EqualTo(SynchronizationOutcome.Cancelled));
		Assert.That(result.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.Captured));
		ResetAdapter();
		Assert.That((await Run()).Revision, Is.EqualTo(1));
	}

	[Test]
	public async Task PreCancelled_DoesNotAcceptOrSubmitChanges()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		SynchronizeInfo result = await Run(cancellation.Token);
		Assert.That(result.Outcome, Is.EqualTo(SynchronizationOutcome.Cancelled));
		Assert.That(result.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.None));
		Assert.That(await ReadPending(), Is.Null);
		using IServiceScope scope = _provider.CreateScope();
		Assert.That((await scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>().CategoryRepo.GetById(_categoryId))!.EditRevision, Is.Null);
		await _adapter.DidNotReceive().Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task InvalidSnapshot_DoesNotReplaceLocalOrAcknowledge()
	{
		_adapter.Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>()).Returns(new DownloadResponse
		{
			Revision = 1, Content = new MemoryStream([1, 2, 3])
		});
		Assert.ThrowsAsync<SqliteException>(() => Run());
		Assert.That((await ReadPending())!.Batches.Count, Is.EqualTo(1));
		using IServiceScope scope = _provider.CreateScope();
		Assert.That((await scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>().LocalConfigRepo.GetAll()).Single().SnapshotRevision, Is.Zero);
		await _adapter.DidNotReceive().Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task FailedAcceptance_RollsBackOutboxAndOriginalTrackingTogether()
	{
		await ExecuteLocal("CREATE TRIGGER reject_accept BEFORE UPDATE ON Category BEGIN SELECT RAISE(ABORT, 'acceptance failed'); END;");
		Assert.ThrowsAsync<SqliteException>(() => Run());
		Assert.That(await ReadPending(), Is.Null);
		using (IServiceScope scope = _provider.CreateScope())
		{
			Assert.That((await scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>().CategoryRepo.GetById(_categoryId))!.EditRevision, Is.Null);
		}
		await _adapter.DidNotReceive().Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>());
		await ExecuteLocal("DROP TRIGGER reject_accept;");
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
	}

	[Test]
	public async Task CancellationDuringAcknowledgement_LeavesInstalledCheckpoint()
	{
		using CancellationTokenSource cancellation = new();
		_adapter.Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>()).Returns(call =>
		{
			cancellation.Cancel();
			return Task.FromCanceled<AcknowledgeResponse>(cancellation.Token);
		});
		SynchronizeInfo stopped = await Run(cancellation.Token);
		Assert.That(stopped.Outcome, Is.EqualTo(SynchronizationOutcome.Cancelled));
		Assert.That(stopped.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.Installed));
		Assert.That((await ReadPending())!.Revision, Is.EqualTo(1));
		ResetAdapter();
		_adapter.ClearReceivedCalls();
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		await _adapter.DidNotReceive().Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task CancellationAfterAcknowledgementSuccess_CompletesCleanup()
	{
		using CancellationTokenSource cancellation = new();
		_adapter.Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			AcknowledgeResponse result = await _real.Acknowledge(call.Arg<AcknowledgeRequest>(), call.Arg<CancellationToken>());
			cancellation.Cancel();
			return result;
		});
		Assert.That((await Run(cancellation.Token)).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		Assert.That((await ReadPending())!.Batches, Is.Empty);
	}

	[Test]
	public async Task DownloadRevisionMismatch_RejectsCandidateAndRetainsSubmittedWork()
	{
		_adapter.Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			DownloadResponse result = await _real.Download(call.Arg<DownloadRequest>(), call.Arg<CancellationToken>());
			return result with { Revision = result.Revision + 1 };
		});
		Assert.ThrowsAsync<InvalidSyncBatchException>(() => Run());
		Assert.That((await ReadPending())!.Batches.Count, Is.EqualTo(1));
		await _adapter.DidNotReceive().Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>());
	}

	private async Task ExecuteLocal(string sql)
	{
		await using SqliteConnection connection = new($"Data Source={LocalPath};Pooling=False");
		await connection.OpenAsync();
		await using SqliteCommand command = connection.CreateCommand();
		command.CommandText = sql;
		await command.ExecuteNonQueryAsync();
	}
	[Test]
	public async Task PreflightAuthorizationFailure_DoesNotCaptureChanges()
	{
		_adapter.CheckAuthorization(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(AuthorizationResult.LoginRequired);
		SynchronizeInfo result = await Run();
		Assert.That(result.Outcome, Is.EqualTo(SynchronizationOutcome.AuthorizationRequired));
		Assert.That(result.Reason, Is.Null);
		Assert.That(result.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.None));
		Assert.That(await ReadPending(), Is.Null);
		await _adapter.DidNotReceive().Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>());
	}

	[TestCase(SynchronizationStopReason.NetworkUnavailable)]
	[TestCase(SynchronizationStopReason.ServerUnavailable)]
	[TestCase(SynchronizationStopReason.Timeout)]
	public async Task PreflightConnectionFailure_DefersWithoutRequestingLogin(SynchronizationStopReason reason)
	{
		_adapter.CheckAuthorization(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<AuthorizationResult>(new MasterRequestException(reason, "Connection failed.")));
		SynchronizeInfo result = await Run();
		Assert.That(result.Outcome, Is.EqualTo(SynchronizationOutcome.Deferred));
		Assert.That(result.Reason, Is.EqualTo(reason));
		Assert.That(result.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.None));
		Assert.That(await ReadPending(), Is.Null);
		await _adapter.DidNotReceive().Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task NoChangesOnEitherSide_SkipsSnapshotTransfer()
	{
		await Run();
		_adapter.ClearReceivedCalls();
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		await _adapter.DidNotReceive().Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>());
	}

	[Test]
	public async Task AccessDenial_PropagatesWithoutAcceptingCapture()
	{
		_adapter.CheckAuthorization(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<AuthorizationResult>(new UnauthorizedAccessException("Dataset denied.")));
		Assert.ThrowsAsync<UnauthorizedAccessException>(() => Run());
		Assert.That(await ReadPending(), Is.Null);
	}
	[Test]
	public async Task BusyBeforeSubmission_RetainsCapturedWorkForLaterSubmission()
	{
		_adapter.Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<SynchronizeResponse>(new MasterBusyException()));
		SynchronizeInfo result = await Run();
		Assert.That(result.Outcome, Is.EqualTo(SynchronizationOutcome.Deferred));
		Assert.That(result.Reason, Is.EqualTo(SynchronizationStopReason.MasterBusy));
		Assert.That(result.Checkpoint, Is.EqualTo(SynchronizationCheckpoint.Captured));
		Assert.That((await ReadPending())!.Batches.Count, Is.EqualTo(1));
		ResetAdapter();
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
	}

	[Test]
	public async Task StoredJson_IsDirectContainerWithoutRecoveryEnvelope()
	{
		await Run();
		using IServiceScope scope = _provider.CreateScope();
		LocalJson row = (await scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>().LocalJsonRepo.GetById(EntitiesIds.LocalJsonId))!;
		using System.Text.Json.JsonDocument document = System.Text.Json.JsonDocument.Parse(row.Json);
		Assert.That(document.RootElement.GetProperty("revision").GetInt64(), Is.EqualTo(1));
		Assert.That(document.RootElement.GetProperty("batches").GetArrayLength(), Is.Zero);
		Assert.That(document.RootElement.TryGetProperty("container", out _), Is.False);
		Assert.That(document.RootElement.TryGetProperty("checkpoint", out _), Is.False);
	}
	[Test]
	public async Task Retry_AppendsNewChangesAndKeepsOriginalBatchIdentity()
	{
		_adapter.Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>())
			.Returns(Task.FromException<SynchronizeResponse>(new MasterBusyException()));
		await Run();
		string originalKey = (await ReadPending())!.Batches.Single().SyncKey;
		using (IServiceScope scope = _provider.CreateScope())
		{
			ILocalAppUnitOfWork local = scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>();
			Category category = (await local.CategoryRepo.GetById(_categoryId))!;
			category.Name = "Edited while offline";
			category.ModificationType = Business.Models.Enums.ModificationType.Content;
			local.CategoryRepo.Update(category);
			await local.SaveChanges();
		}
		ResetAdapter();
		Assert.That((await Run()).Outcome, Is.EqualTo(SynchronizationOutcome.Complete));
		using IServiceScope verification = _provider.CreateScope();
		IMasterAppUnitOfWork master = verification.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		ICollection<LocalSyncBatch> receipts = await master.LocalSyncBatchRepo.GetAll();
		Assert.That(receipts.Count, Is.EqualTo(2));
		Assert.That(receipts.Any(receipt => receipt.SyncKey == originalKey), Is.True);
		Assert.That((await master.CategoryRepo.GetById(_categoryId))!.Name, Is.EqualTo("Edited while offline"));
		Assert.That((await ReadPending())!.Batches, Is.Empty);
	}

	[Test]
	public async Task MissingReceipt_DoesNotPruneActiveContainerOrInstallCandidate()
	{
		_adapter.Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>()).Returns(async call =>
		{
			await using SqliteConnection connection = new($"Data Source={MasterPath};Pooling=False");
			await connection.OpenAsync();
			await using SqliteCommand command = connection.CreateCommand();
			command.CommandText = "DELETE FROM LocalSyncBatch;";
			await command.ExecuteNonQueryAsync();
			return await _real.Download(call.Arg<DownloadRequest>(), call.Arg<CancellationToken>());
		});
		Assert.ThrowsAsync<InvalidSyncBatchException>(() => Run());
		SyncContainer container = (await ReadPending())!;
		Assert.That(container.Batches.Count, Is.EqualTo(1));
		Assert.That(container.Revision, Is.Zero);
		using IServiceScope scope = _provider.CreateScope();
		LocalConfig local = (await scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>().LocalConfigRepo.GetAll()).Single();
		Assert.That(local.SnapshotRevision, Is.Zero);
		await _adapter.DidNotReceive().Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>());
	}
	private void ResetAdapter()
	{
		_adapter.CheckAuthorization(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
			.Returns(call => _real.CheckAuthorization(
				call.ArgAt<string>(0),
				call.ArgAt<string>(1),
				call.Arg<CancellationToken>()));
		_adapter.Synchronize(Arg.Any<SynchronizeRequest>(), Arg.Any<CancellationToken>()).Returns(call => _real.Synchronize(call.Arg<SynchronizeRequest>(), call.Arg<CancellationToken>()));
		_adapter.Download(Arg.Any<DownloadRequest>(), Arg.Any<CancellationToken>()).Returns(call => _real.Download(call.Arg<DownloadRequest>(), call.Arg<CancellationToken>()));
		_adapter.Acknowledge(Arg.Any<AcknowledgeRequest>(), Arg.Any<CancellationToken>()).Returns(call => _real.Acknowledge(call.Arg<AcknowledgeRequest>(), call.Arg<CancellationToken>()));
	}

	private async Task<SynchronizeInfo> Run(CancellationToken token = default)
	{
		await using AsyncServiceScope scope = _provider.CreateAsyncScope();
		return await scope.ServiceProvider.GetRequiredService<ISynchronizationService>().Synchronize(token);
	}

	private async Task<SyncContainer?> ReadPending()
	{
		using IServiceScope scope = _provider.CreateScope();
		LocalJson? row = await scope.ServiceProvider.GetRequiredService<ILocalAppUnitOfWork>().LocalJsonRepo.GetById(EntitiesIds.LocalJsonId);
		return row is null || row.Json.Length == 0 ? null : scope.ServiceProvider.GetRequiredService<ISharedContext>().JsonService.Deserialize<SyncContainer>(row.Json);
	}
}
