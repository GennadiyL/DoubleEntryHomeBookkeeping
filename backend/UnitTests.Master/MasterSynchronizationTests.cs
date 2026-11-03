using System.Reflection;
using DataAccess.EntityFramework;
using DataAccess.Core.EntityFramework.Entities;
using System.Text.Json;
using Business.Models.Constants;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.EntityFramework.SqLite;
using Exchange.Models.Enums;
using Exchange.Models.Synchronizations;
using Shared.Contracts.Storage;
using Master.Contracts.Services;
using Master.Contracts.Services.Synchronizations;
using Master.Impl;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Impl;
using Master.Impl.Operations.MasterStorage;

namespace UnitTests.Master;

/// <summary>
/// Verifies Master submission orchestration against the production SQLite DAL.
/// Resolves the public Master service through its normal scoped registration.
/// Tests ordered application, immutable receipts and revision allocation together.
/// Injected database failures occur after the merge has saved entity changes.
/// Cancellation cases verify rollback without a cancelled cleanup token.
/// Storage access contention is covered by the companion access tests.
/// Each test owns an isolated database and fixed acknowledgement timestamps.
/// No existing user database or remote infrastructure is used.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the provider and database connection.")]
[TestFixture(Category = "Local")]
public sealed class MasterSynchronizationTests
{
	private MasterTestStore _store = null!;

	private ServiceProvider _provider = null!;
	private string _masterKey = null!;
	private Guid _localId;
	private static readonly DateTime Now = new(2026, 10, 22, 12, 0, 0, DateTimeKind.Utc);

	[SetUp]
	public async Task SetUp()
	{
		_store = new MasterTestStore();
		await _store.Initialize();
		_provider = _store.CreateProvider();
		using IServiceScope scope = _provider.CreateScope();
		IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
		_masterKey = "master";
		_localId = Guid.NewGuid();
		unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = _localId, LocalDatasetKey = "local-one", SyncDateTime = Now.AddDays(-1) });
		await unit.SaveChanges();
	}

	[TearDown]
	public void TearDown()
	{
		_provider?.Dispose();
		_store?.Dispose();
	}

	[Test]
	public async Task EachCommittedBatchPublishesSnapshotAndRemovesWorkingDirectory()
	{
		await Synchronize(Request(Batch("first"), Batch("second")));
		Assert.That(File.Exists(Path.Combine(_store.DatasetDirectory, "snapshot_1.db")), Is.True);
		Assert.That(File.Exists(Path.Combine(_store.DatasetDirectory, "snapshot_2.db")), Is.True);
		Assert.That(Directory.GetDirectories(_store.TempRoot), Is.Empty);
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(2));
	}

	[Test]
	public async Task OrderedBatches_CommitChangesReceiptsAndRevisionsWithoutAcknowledging()
	{
		SyncBatch first = Batch("first");
		SyncObject category = Category(Guid.NewGuid(), "First");
		first.Added.Add(category);
		SyncBatch second = Batch("second");
		second.CreatedAt = first.CreatedAt.AddDays(-1);
		second.Updated.Add(Category(category.Id, "Second", 0));
		SynchronizeResponse result = await Synchronize(Request(first, second));
		Assert.That(result.Revision, Is.EqualTo(2));
		using IServiceScope scope = _provider.CreateScope();
		IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
		Assert.That((await unit.CategoryRepo.GetById(category.Id))!.Name, Is.EqualTo("Second"));
		Assert.That((await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(2));
		LocalSyncBatch[] receipts = (await unit.LocalSyncBatchRepo.GetAll()).OrderBy(item => item.Revision).ToArray();
		Assert.That(receipts, Has.Length.EqualTo(2));
		Assert.That(receipts[0].SyncKey, Is.EqualTo("first"));
		Assert.That(receipts[1].SyncKey, Is.EqualTo("second"));
		Assert.That(receipts.Select(item => item.PayloadHash.Length), Is.All.EqualTo(64));
		LocalRegistration local = (await unit.LocalRegistrationRepo.GetById(_localId))!;
		Assert.That(local.SyncRevision, Is.Zero);
		Assert.That(local.SyncDateTime, Is.EqualTo(Now.AddDays(-1)));
	}

	[Test]
	public async Task RetryAfterLostResponse_SkipsOldBatchAndAppliesNextBatch()
	{
		SyncBatch first = Batch("first");
		first.Added.Add(Category(Guid.NewGuid(), "First"));
		await Synchronize(Request(first));
		SyncBatch second = Batch("second");
		second.Added.Add(Category(Guid.NewGuid(), "Second"));
		Assert.That((await Synchronize(Request(first, second))).Revision, Is.EqualTo(2));
		Assert.That((await Synchronize(Request(first, second))).Revision, Is.EqualTo(2));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(2));
		Assert.That(await Scalar("SELECT COUNT(*) FROM Category"), Is.EqualTo(2));
	}

	[Test]
	public async Task ReorderedJsonProperties_HasSameReceiptFingerprint()
	{
		SyncBatch batch = Batch("retry");
		SyncObject category = Category(Guid.NewGuid(), "Name");
		batch.Added.Add(category);
		await Synchronize(Request(batch));
		KeyValuePair<string, JsonElement>[] properties = category.Properties.Reverse().ToArray();
		category.Properties.Clear();
		foreach (KeyValuePair<string, JsonElement> property in properties) { category.Properties.Add(property.Key, property.Value); }
		Assert.That((await Synchronize(Request(batch))).Revision, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ExistingKeyWithChangedPayload_IsRejected(bool changePriority)
	{
		SyncBatch batch = Batch("retry");
		batch.Added.Add(Category(Guid.NewGuid(), "Original"));
		await Synchronize(Request(batch));
		if (changePriority) { batch.Priority = ConflictPriority.Master; }
		else { batch.Added[0].Properties["name"] = JsonSerializer.SerializeToElement("Changed"); }
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Synchronize(Request(batch)));
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(1));
	}

	[Test]
	public async Task SameKeyFromDifferentLocals_HasSeparateReceipts()
	{
		using (IServiceScope scope = _provider.CreateScope())
		{
			IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
			unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "local-two", SyncDateTime = Now });
			await unit.SaveChanges();
		}
		SyncBatch batch = Batch("same-key");
		await Synchronize(Request(batch));
		SynchronizeRequest request = Request(batch);
		request.Container.LocalDatasetKey = "local-two";
		Assert.That((await Synchronize(request)).Revision, Is.EqualTo(2));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(2));
	}

	[Test]
	public async Task EmptyContainer_ReturnsCurrentRevisionWithoutReceipt()
	{
		Assert.That((await Synchronize(Request())).Revision, Is.Zero);
		await Synchronize(Request(Batch("first")));
		Assert.That((await Synchronize(Request())).Revision, Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(1));
	}

	[TestCase("format")]
	[TestCase("schema")]
	[TestCase("master")]
	[TestCase("local")]
	public void InvalidContainer_IsRejected(string field)
	{
		SynchronizeRequest request = Request();
		switch (field)
		{
			case "format": request.Container.FormatVersion = 2; break;
			case "schema": request.Container.SchemaVersion = 2; break;
			case "master":
				request.Container.MasterDatasetKey = "other-master";
				string otherDirectory = Path.Combine(_store.StorageRoot, "Master_other-master");
				Directory.CreateDirectory(otherDirectory);
				File.Copy(_store.DatabasePath, Path.Combine(otherDirectory, "master.db"));
				break;
			case "local": request.Container.LocalDatasetKey = "unknown-local"; break;
		}
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Synchronize(request));
	}

	[TestCase(89)]
	[TestCase(90)]
	[TestCase(91)]
	[TestCase(3650)]
	public async Task InactiveLocal_CanMergeAndRetryWithoutRenewingAcknowledgement(int days)
	{
		DateTime acknowledgedAt = Now.AddDays(-days);
		await SetAcknowledgement(acknowledgedAt);
		SyncBatch batch = Batch("old-local");
		batch.CreatedAt = acknowledgedAt;
		SyncObject category = Category(Guid.NewGuid(), "Offline changes");
		batch.Added.Add(category);
		Assert.That((await Synchronize(Request(batch))).Revision, Is.EqualTo(1));
		Assert.That((await Synchronize(Request(batch))).Revision, Is.EqualTo(1));
		using IServiceScope scope = _provider.CreateScope();
		IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
		Assert.That((await unit.CategoryRepo.GetById(category.Id))!.Name, Is.EqualTo("Offline changes"));
		Assert.That((await unit.LocalRegistrationRepo.GetById(_localId))!.SyncDateTime, Is.EqualTo(acknowledgedAt));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(1));
	}

	[Test]
	public async Task InitialUnacknowledgedRegistration_CanSynchronize()
	{
		await SetAcknowledgement(null);
		Assert.That((await Synchronize(Request())).Revision, Is.Zero);
	}

	[Test]
	public async Task LateReceiptFailure_RollsBackOnlyCurrentBatchAndRetryCompletes()
	{
		await Execute("CREATE TRIGGER fail_receipt BEFORE INSERT ON LocalSyncBatch WHEN NEW.SyncKey = 'second' BEGIN SELECT RAISE(ABORT, 'receipt rejected'); END;");
		SyncBatch first = Batch("first");
		first.Added.Add(Category(Guid.NewGuid(), "First"));
		SyncBatch second = Batch("second");
		second.Added.Add(Category(Guid.NewGuid(), "Second"));
		SynchronizeRequest request = Request(first, second);
		Assert.ThrowsAsync<DbUpdateException>(async () => await Synchronize(request));
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM Category"), Is.EqualTo(1));
		await Execute("DROP TRIGGER fail_receipt;");
		Assert.That((await Synchronize(request)).Revision, Is.EqualTo(2));
		Assert.That(await Scalar("SELECT COUNT(*) FROM Category"), Is.EqualTo(2));
	}

	[Test]
	public async Task CancelledDuringReceipt_RollsBackCurrentBatchAndKeepsPreviousCommit()
	{
		SyncBatch first = Batch("first");
		first.Added.Add(Category(Guid.NewGuid(), "First"));
		SyncBatch second = Batch("second");
		second.Added.Add(Category(Guid.NewGuid(), "Second"));
		await Execute("CREATE TRIGGER cancel_receipt BEFORE INSERT ON LocalSyncBatch WHEN NEW.SyncKey = 'second' BEGIN SELECT cancel_sync(); END;");
		using CancellationTokenSource cancellation = new();
		using (IServiceScope scope = _provider.CreateScope())
		{
			IMasterUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IUnitOfWorkFactory>().CreateMaster(_masterKey);
			AppDbContext context = (AppDbContext)typeof(UnitOfWork<AppDbContext>)
				.GetProperty("Context", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(unit)!;
			((SqliteConnection)context.Database.GetDbConnection()).CreateFunction("cancel_sync", () => { cancellation.Cancel(); return 0; });
			IMasterService service = scope.ServiceProvider.GetRequiredService<IMasterService>();
			Assert.CatchAsync<OperationCanceledException>(async () => await service.Synchronize(Request(first, second), cancellation.Token));
		}
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM Category"), Is.EqualTo(1));
		await Execute("DROP TRIGGER cancel_receipt;");
		Assert.That((await Synchronize(Request(first, second))).Revision, Is.EqualTo(2));
	}

	[Test]
	public async Task AlreadyCancelled_WritesNothing()
	{
		using CancellationTokenSource cancellation = new();
		await cancellation.CancelAsync();
		using IServiceScope scope = _provider.CreateScope();
		IMasterService service = scope.ServiceProvider.GetRequiredService<IMasterService>();
		Assert.CatchAsync<OperationCanceledException>(async () => await service.Synchronize(Request(Batch("first")), cancellation.Token));
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.Zero);
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.Zero);
	}

	[Test]
	public async Task InvalidMiddleBatch_StopsBeforeFollowingBatch()
	{
		SyncBatch first = Batch("first");
		first.Added.Add(Category(Guid.NewGuid(), "First"));
		SyncBatch invalid = Batch("invalid");
		invalid.Added.Add(Category(Guid.NewGuid(), "Invalid"));
		invalid.Added[0].Type = "unknown-type";
		SyncBatch last = Batch("last");
		last.Added.Add(Category(Guid.NewGuid(), "Last"));
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Synchronize(Request(first, invalid, last)));
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.EqualTo(1));
		Assert.That(await Scalar("SELECT COUNT(*) FROM Category"), Is.EqualTo(1));
	}

	[Test]
	public async Task ExhaustedRevision_RejectsNewBatchWithoutWrapping()
	{
		using (IServiceScope scope = _provider.CreateScope())
		{
			IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision = long.MaxValue;
			unit.SystemConfigRepo.Update(config);
			await unit.SaveChanges();
		}
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Synchronize(Request(Batch("overflow"))));
		Assert.That(await Scalar("SELECT CurrentRevision FROM SystemConfig"), Is.EqualTo(long.MaxValue));
		Assert.That(await Scalar("SELECT COUNT(*) FROM LocalSyncBatch"), Is.Zero);
	}

	private async Task<SynchronizeResponse> Synchronize(SynchronizeRequest request)
	{
		using IServiceScope scope = _provider.CreateScope();
		return await scope.ServiceProvider.GetRequiredService<IMasterService>().Synchronize(request);
	}

	private SynchronizeRequest Request(params SyncBatch[] batches)
	{
		SyncContainer container = new() { FormatVersion = 1, SchemaVersion = 1, MasterDatasetKey = _masterKey, LocalDatasetKey = "local-one" };
		container.Batches.AddRange(batches);
		return new SynchronizeRequest { Container = container };
	}

	private static SyncBatch Batch(string key) => new() { SyncKey = key, CreatedAt = Now, Priority = ConflictPriority.Local };

	private static SyncObject Category(Guid id, string name, long? revision = null)
	{
		SyncObject item = new() { Type = "category", Id = id, EditRevision = revision, ModificationType = ModificationType.Content };
		item.Properties.Add("name", JsonSerializer.SerializeToElement(name));
		item.Properties.Add("description", JsonSerializer.SerializeToElement((string?)null));
		item.Properties.Add("isFavorite", JsonSerializer.SerializeToElement(false));
		item.Properties.Add("groupId", JsonSerializer.SerializeToElement(RootsIds.CategoryGroupId));
		if (revision is null) { item.Properties.Add("order", JsonSerializer.SerializeToElement(0)); }
		return item;
	}

	private async Task SetAcknowledgement(DateTime? dateTime)
	{
		using IServiceScope scope = _provider.CreateScope();
		IMasterUnitOfWork unit = _store.OpenMaster(scope.ServiceProvider);
		LocalRegistration local = (await unit.LocalRegistrationRepo.GetById(_localId))!;
		local.SyncDateTime = dateTime;
		unit.LocalRegistrationRepo.Update(local);
		await unit.SaveChanges();
	}

	private Task Execute(string sql) => _store.Execute(sql);

	private Task<long> Scalar(string sql) => _store.Scalar(sql);
}
