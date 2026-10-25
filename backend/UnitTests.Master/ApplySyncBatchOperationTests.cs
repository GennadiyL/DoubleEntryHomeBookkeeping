using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Entities;
using DataAccess.EntityFramework.SqLite;
using Exchange.Models.Synchronizations;
using Master.Impl;
using Master.Impl.Operations.ApplySyncBatch;
using Microsoft.Data.Sqlite;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared.Impl;
using ConflictPriority = Exchange.Models.Enums.ConflictPriority;
using ModificationType = Exchange.Models.Enums.ModificationType;
using StoredModificationType = Business.Models.Enums.ModificationType;

namespace UnitTests.Master;

/// <summary>
/// Exercises one Master merge operation against isolated SQLite databases.
/// Uses production schema, repositories, mapping and DI registrations.
/// The fixture acts as the caller owning revisions, receipts and transactions.
/// Verifies conflict provenance, natural rate keys and aggregate replacement.
/// Covers dependency repair, protected rows, hierarchy and independent ordering.
/// Failure cases check rollback after both validation and database writes.
/// Sequential batches share a scope to expose persistence tracking problems.
/// No user database, network endpoint or Azure host is accessed.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the provider and database connection.")]
[TestFixture(Category = "Local")]
public sealed class ApplySyncBatchOperationTests
{
	private SqliteConnection _connection = null!;
	private ServiceProvider _provider = null!;
	private Guid _local;
	private Guid _other;
	private Guid _baseCurrency;
	private static readonly JsonSerializerOptions _json = new() { Converters = { new JsonStringEnumConverter() } };

	[SetUp]
	public async Task SetUp()
	{
		_connection = new SqliteConnection($"Data Source=master-{Guid.NewGuid():N};Mode=Memory;Cache=Shared;Pooling=False");
		await _connection.OpenAsync();
		foreach (string script in new[] { "001-schema.sql", "002-initial-data.sql" })
		{
			await using SqliteCommand command = _connection.CreateCommand();
			command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", script));
			await command.ExecuteNonQueryAsync();
		}
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddMasterDataAccessSqLiteModule(new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ConnectionStrings:DehbMasterSqLite"] = _connection.ConnectionString
		}).Build());
		services.AddMasterModule();
		services.AddLocalStorageModule();
		services.AddLocalMasterStorage(_ => _connection.DataSource);
		_provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		_local = Guid.NewGuid();
		_other = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			_baseCurrency = config.BaseCurrencyId;
			config.CurrentRevision = 10;
			unit.SystemConfigRepo.Update(config);
			unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = _local, LocalDatasetKey = "local-one" });
			unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = _other, LocalDatasetKey = "local-two" });
			unit.LocalSyncBatchRepo.Add(Receipt("previous-one", 9, _local));
			unit.LocalSyncBatchRepo.Add(Receipt("previous-two", 10, _other));
			await unit.SaveChanges();
		});
	}

	[TearDown]
	public void TearDown()
	{
		_provider?.Dispose();
		_connection?.Dispose();
	}

	[TestCase(ConflictPriority.Master, 10, false, true)]
	[TestCase(ConflictPriority.Master, 9, false, false)]
	[TestCase(ConflictPriority.Local, 9, false, true)]
	[TestCase(ConflictPriority.Master, 0, true, true)]
	[TestCase(ConflictPriority.Master, 9, true, true)]
	public async Task Content_UsesRevisionPriorityAndCurrentEntitySource(ConflictPriority priority, long incomingRevision, bool sameLocal, bool wins)
	{
		Guid id = await SeedCategory();
		if (sameLocal)
		{
			await WithUnit(async unit =>
			{
				LocalSyncBatch receipt = (await unit.LocalSyncBatchRepo.GetAll()).Single(value => value.Revision == 10);
				receipt.LocalRegistrationId = _local;
				unit.LocalSyncBatchRepo.Update(receipt);
				await unit.SaveChanges();
			});
		}
		SyncBatch batch = Batch(priority);
		batch.Updated.Add(Catalog("category", id, "Local", RootsIds.CategoryGroupId, incomingRevision));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Category row = (await unit.CategoryRepo.GetById(id))!;
			Assert.That((row.Name, row.EditRevision, row.ModificationType), Is.EqualTo((wins ? "Local" : "Master", (long?)(wins ? 11 : 10), StoredModificationType.None)));
		});
	}

	[Test]
	public async Task SameLocal_UsesEntityRevisionSourceInsteadOfLatestGlobalReceipt()
	{
		Guid id = await SeedCategory(revision: 9);
		SyncBatch batch = Batch(ConflictPriority.Master);
		batch.Updated.Add(Catalog("category", id, "Newest", RootsIds.CategoryGroupId, 8));
		await Apply(batch);
		await WithUnit(async unit => Assert.That((await unit.CategoryRepo.GetById(id))!.Name, Is.EqualTo("Newest")));
	}

	[Test]
	public async Task UnchangedContent_DoesNotAdvanceEntityRevision()
	{
		Guid id = await SeedCategory();
		SyncBatch batch = Batch();
		batch.Updated.Add(Catalog("category", id, "Master", RootsIds.CategoryGroupId, 10));
		await Apply(batch);
		await WithUnit(async unit => Assert.That((await unit.CategoryRepo.GetById(id))!.EditRevision, Is.EqualTo(10)));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task Order_WinsIndependentlyWithoutChangingContentRevision(bool contentFlag)
	{
		Guid first = await SeedCategory("First", 0);
		Guid second = await SeedCategory("Second", 1);
		SyncBatch batch = Batch(ConflictPriority.Master);
		SyncObject item = contentFlag
			? Catalog("category", second, "Rejected", RootsIds.CategoryGroupId, 8, -1, ModificationType.Content | ModificationType.Order)
			: Object("category", second, 8, ModificationType.Order, ("order", -1));
		batch.Updated.Add(item);
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Category row = (await unit.CategoryRepo.GetById(second))!;
			Assert.That((row.Name, row.Order, row.EditRevision), Is.EqualTo(("Second", 0, (long?)10)));
			Assert.That((await unit.CategoryRepo.GetById(first))!.Order, Is.EqualTo(1));
			Assert.That((await unit.CategoryRepo.GetById(first))!.EditRevision, Is.EqualTo(10));
		});
	}

	[Test]
	public async Task DeletionAndRestoration_KeepIdentityAndStampBothRevisions()
	{
		Guid id = await SeedCategory();
		SyncBatch deletion = Batch();
		SyncObject deleted = Catalog("category", id, "Master", RootsIds.CategoryGroupId, 10);
		deleted.Properties.Add("order", JsonSerializer.SerializeToElement(0));
		deletion.Deleted.Add(deleted);
		await Apply(deletion);
		await WithUnit(async unit =>
		{
			Category row = (await unit.CategoryRepo.GetById(id))!;
			Assert.That((row.EditRevision, row.DeleteRevision), Is.EqualTo(((long?)11, (long?)11)));
		});
		SyncBatch restoration = Batch(ConflictPriority.Master);
		restoration.Updated.Add(Catalog("category", id, "Restored", RootsIds.CategoryGroupId, 10));
		await Apply(restoration, 12);
		await WithUnit(async unit =>
		{
			Category row = (await unit.CategoryRepo.GetById(id))!;
			Assert.That((row.Name, row.EditRevision, row.DeleteRevision), Is.EqualTo(("Restored", (long?)12, (long?)null)));
		});
	}

	[TestCase(ConflictPriority.Local, "Local", 11)]
	[TestCase(ConflictPriority.Master, "UAH", 0)]
	public async Task ConcurrentCurrencyCreation_UsesPermanentIdentity(ConflictPriority priority, string name, long revision)
	{
		SyncBatch batch = Batch(priority);
		batch.Added.Add(CurrencyObject(_baseCurrency, "UAH", "Local"));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That(await unit.CurrencyRepo.GetAll(), Has.Count.EqualTo(1));
			Currency row = (await unit.CurrencyRepo.GetById(_baseCurrency))!;
			Assert.That((row.Name, row.EditRevision), Is.EqualTo((name, (long?)revision)));
		});
	}

	[TestCase(ConflictPriority.Local, null, false, 1.5)]
	[TestCase(ConflictPriority.Master, null, false, 1.0)]
	[TestCase(ConflictPriority.Master, 9L, false, 1.0)]
	[TestCase(ConflictPriority.Master, 10L, false, 1.5)]
	[TestCase(ConflictPriority.Local, 9L, true, 1.5)]
	public async Task CurrencyRate_MatchesCurrencyAndDateInsteadOfIncomingId(ConflictPriority priority, long? editRevision, bool delete, double expected)
	{
		Guid currencyId = await SeedForeignCurrency();
		Guid rateId = Guid.NewGuid();
		DateOnly date = new(2026, 10, 20);
		await WithUnit(async unit =>
		{
			unit.CurrencyRateRepo.Add(new CurrencyRate { Id = rateId, CurrencyId = currencyId, Currency = null!, Date = date, Rate = 1m, EditRevision = 10 });
			await unit.SaveChanges();
		});
		SyncBatch batch = Batch(priority);
		SyncObject item = RateObject(Guid.NewGuid(), currencyId, date, 1.5m, editRevision);
		if (delete) { batch.Deleted.Add(item); }
		else if (editRevision is null) { batch.Added.Add(item); }
		else { batch.Updated.Add(item); }
		await Apply(batch);
		await WithUnit(async unit =>
		{
			CurrencyRate row = (await unit.CurrencyRateRepo.GetByCurrencyAndDate(currencyId, date))!;
			Assert.That((row.Id, row.Rate, row.DeleteRevision), Is.EqualTo((rateId, (decimal)expected, delete ? (long?)11 : null)));
			Assert.That((await unit.CurrencyRateRepo.GetAll()).Count(rate => rate.CurrencyId == currencyId), Is.EqualTo(2));
		});
	}

	[Test]
	public async Task AllEntityTypes_ApplyInDependencyOrderRegardlessOfPayloadOrder()
	{
		Guid currencyId = Guid.NewGuid();
		Guid accountId = Guid.NewGuid();
		Guid categoryId = Guid.NewGuid();
		Guid correspondentId = Guid.NewGuid();
		Guid projectId = Guid.NewGuid();
		Guid templateId = Guid.NewGuid();
		Guid reportId = Guid.NewGuid();
		Guid transactionId = Guid.NewGuid();
		Dictionary<string, Guid> groups = ((string[])["account", "category", "correspondent", "project", "template", "report"]).ToDictionary(type => type, _ => Guid.NewGuid());
		SyncBatch batch = Batch();
		batch.Added.Add(TransactionObject(transactionId, null, [Entry(accountId, 3m, 0), Entry(accountId, -3m, 1)]));
		SyncObject template = Catalog("template", templateId, "Template", groups["template"]);
		template.Properties.Add("entries", JsonSerializer.SerializeToElement(new[] { new { id = Guid.NewGuid(), accountId, position = 0, amount = 1.2345m } }));
		batch.Added.Add(template);
		SyncObject report = Catalog("report", reportId, "Report", groups["report"]);
		report.Properties.Add("json", JsonSerializer.SerializeToElement("{}"));
		batch.Added.Add(report);
		SyncObject account = AccountObject(accountId, groups["account"], currencyId);
		account.Properties["categoryId"] = JsonSerializer.SerializeToElement(categoryId);
		account.Properties["correspondentId"] = JsonSerializer.SerializeToElement(correspondentId);
		account.Properties["projectId"] = JsonSerializer.SerializeToElement(projectId);
		batch.Added.Add(account);
		batch.Added.Add(Catalog("category", categoryId, "Category", groups["category"]));
		batch.Added.Add(Catalog("correspondent", correspondentId, "Correspondent", groups["correspondent"]));
		batch.Added.Add(Catalog("project", projectId, "Project", groups["project"]));
		batch.Added.Add(RateObject(Guid.NewGuid(), currencyId, AppValues.InitialDate, 1m));
		batch.Added.Add(CurrencyObject(currencyId, "EUR", "Euro"));
		Guid[] roots = [RootsIds.AccountGroupId, RootsIds.CategoryGroupId, RootsIds.CorrespondentGroupId, RootsIds.ProjectGroupId, RootsIds.TemplateGroupId, RootsIds.ReportGroupId];
		int index = 0;
		foreach ((string type, Guid id) in groups) { batch.Added.Add(Catalog(type + "Group", id, "Group", roots[index++])); }
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.TransactionRepo.GetById(transactionId))!.State, Is.EqualTo(TransactionState.Confirmed));
			Assert.That((await unit.TransactionEntryRepo.GetByTransactionId(transactionId)).Select(entry => entry.CumulativeAmount), Is.EquivalentTo((decimal[])[3m, 0m]));
			Assert.That((await unit.TemplateEntryRepo.GetByTemplateId(templateId)).Single().Amount, Is.EqualTo(1.23m));
			Assert.That((await unit.ReportRepo.GetById(reportId))!.Json, Is.EqualTo("{}"));
			Assert.That((await unit.AccountRepo.GetById(accountId))!.CurrencyId, Is.EqualTo(currencyId));
			Assert.That((await unit.CategoryRepo.GetById(categoryId))!.EditRevision, Is.EqualTo(11));
			Assert.That((await unit.CorrespondentRepo.GetById(correspondentId))!.EditRevision, Is.EqualTo(11));
			Assert.That((await unit.ProjectRepo.GetById(projectId))!.EditRevision, Is.EqualTo(11));
		});
	}

	[Test]
	public async Task NewEntry_RestoresDeletedAccountCurrencyAndAncestorChain()
	{
		Guid currencyId = await SeedForeignCurrency();
		Guid parentId = Guid.NewGuid();
		Guid childId = Guid.NewGuid();
		Guid accountId = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			Currency currency = (await unit.CurrencyRepo.GetById(currencyId))!;
			currency.DeleteRevision = 10;
			unit.CurrencyRepo.Update(currency);
			unit.AccountGroupRepo.Add(new AccountGroup { Id = parentId, ParentId = RootsIds.AccountGroupId, Name = "Parent", DeleteRevision = 10, EditRevision = 10 });
			unit.AccountGroupRepo.Add(new AccountGroup { Id = childId, ParentId = parentId, Name = "Child", DeleteRevision = 10, EditRevision = 10 });
			unit.AccountRepo.Add(new Account { Id = accountId, GroupId = childId, CurrencyId = currencyId, Name = "Account", DeleteRevision = 10, EditRevision = 10 });
			await unit.SaveChanges();
		});
		SyncBatch batch = Batch(ConflictPriority.Master);
		batch.Added.Add(TransactionObject(Guid.NewGuid(), null, [Entry(accountId, 1m, 0)]));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.CurrencyRepo.GetById(currencyId))!.DeleteRevision, Is.Null);
			Assert.That((await unit.AccountRepo.GetById(accountId))!.DeleteRevision, Is.Null);
			Assert.That((await unit.AccountGroupRepo.GetById(childId))!.ParentId, Is.EqualTo(parentId));
			Assert.That((await unit.AccountGroupRepo.GetById(childId))!.DeleteRevision, Is.Null);
			Assert.That((await unit.AccountGroupRepo.GetById(parentId))!.EditRevision, Is.EqualTo(11));
			Assert.That((await unit.TransactionRepo.GetAll()).Single().State, Is.EqualTo(TransactionState.Draft));
		});
	}

	[TestCase(ConflictPriority.Local)]
	[TestCase(ConflictPriority.Master)]
	public async Task HierarchyCycle_PreservesPreferredEdge(ConflictPriority priority)
	{
		Guid parent = Guid.NewGuid();
		Guid child = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			unit.CategoryGroupRepo.Add(new CategoryGroup { Id = parent, Name = "Parent", ParentId = RootsIds.CategoryGroupId, EditRevision = 10 });
			unit.CategoryGroupRepo.Add(new CategoryGroup { Id = child, Name = "Child", ParentId = parent, EditRevision = 10 });
			await unit.SaveChanges();
		});
		SyncBatch batch = Batch(priority);
		batch.Updated.Add(Catalog("categoryGroup", parent, "Parent", child, 10));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.CategoryGroupRepo.GetById(parent))!.ParentId, Is.EqualTo(priority == ConflictPriority.Local ? child : RootsIds.CategoryGroupId));
			Assert.That((await unit.CategoryGroupRepo.GetById(child))!.ParentId, Is.EqualTo(priority == ConflictPriority.Local ? RootsIds.CategoryGroupId : parent));
		});
	}

	[TestCase(ConflictPriority.Local)]
	[TestCase(ConflictPriority.Master)]
	public async Task DuplicateNames_KeepPreferredAndSuffixOther(ConflictPriority priority)
	{
		Guid original = await SeedCategory("Same");
		Guid added = Guid.NewGuid();
		SyncBatch batch = Batch(priority);
		batch.Added.Add(Catalog("category", added, " Same ", RootsIds.CategoryGroupId));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.CategoryRepo.GetById(original))!.Name, Is.EqualTo(priority == ConflictPriority.Local ? "Same_1" : "Same"));
			Assert.That((await unit.CategoryRepo.GetById(added))!.Name, Is.EqualTo(priority == ConflictPriority.Local ? "Same" : "Same_1"));
		});
	}

	[Test]
	public async Task SequentialBatches_ReplaceAggregatesInSameScope()
	{
		Guid transactionId = Guid.NewGuid();
		Guid templateId = Guid.NewGuid();
		SyncBatch first = Batch();
		first.Added.Add(TransactionObject(transactionId, null, [Entry(EntitiesIds.BalancingAccountId, 1m, 0), Entry(EntitiesIds.BalancingAccountId, -1m, 1)]));
		SyncObject template = Catalog("template", templateId, "Template", RootsIds.TemplateGroupId);
		template.Properties.Add("entries", JsonSerializer.SerializeToElement(new[] { new { id = Guid.NewGuid(), accountId = EntitiesIds.BalancingAccountId, position = 0, amount = 2m } }));
		first.Added.Add(template);
		SyncBatch second = Batch(ConflictPriority.Master);
		SyncObject replacement = TransactionObject(transactionId, 0, [Entry(EntitiesIds.BalancingAccountId, 5m, 0), Entry(EntitiesIds.BalancingAccountId, -5m, 1)]);
		second.Updated.Add(replacement);
		SyncObject updatedTemplate = Catalog("template", templateId, "Template", RootsIds.TemplateGroupId, 0);
		updatedTemplate.Properties.Add("entries", JsonSerializer.SerializeToElement(Array.Empty<object>()));
		second.Updated.Add(updatedTemplate);
		using (IServiceScope scope = _provider.CreateScope())
		{
			await ApplyInScope(scope, first, 11);
			await ApplyInScope(scope, second, 12);
		}
		await WithUnit(async unit =>
		{
			Assert.That((await unit.TransactionEntryRepo.GetByTransactionId(transactionId)).Select(entry => entry.Amount), Is.EquivalentTo(new[] { 5m, -5m }));
			Assert.That((await unit.TransactionRepo.GetById(transactionId))!.EditRevision, Is.EqualTo(12));
			Assert.That(await unit.TemplateEntryRepo.GetByTemplateId(templateId), Is.Empty);
		});
	}

	[TestCase("unknownType")]
	[TestCase("missingField")]
	[TestCase("unknownField")]
	[TestCase("missingParent")]
	[TestCase("duplicateObject")]
	[TestCase("invalidFlags")]
	public async Task InvalidBatch_LeavesDatabaseAndRevisionUnchanged(string scenario)
	{
		SyncBatch batch = Batch();
		SyncObject item = Catalog("category", Guid.NewGuid(), "Valid", RootsIds.CategoryGroupId);
		batch.Added.Add(item);
		switch (scenario)
		{
			case "unknownType": item.Type = "systemConfig"; break;
			case "missingField": item.Properties.Remove("name"); break;
			case "unknownField": item.Properties.Add("deleteRevision", JsonSerializer.SerializeToElement(99)); break;
			case "missingParent": item.Properties["groupId"] = JsonSerializer.SerializeToElement(Guid.NewGuid()); break;
			case "duplicateObject": batch.Added.Add(item); break;
			case "invalidFlags": item.ModificationType = (ModificationType)8; break;
		}
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Apply(batch));
		await WithUnit(async unit =>
		{
			Assert.That(await unit.CategoryRepo.GetAll(), Is.Empty);
			Assert.That((await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(10));
			Assert.That(await unit.LocalSyncBatchRepo.GetAll(), Has.Count.EqualTo(2));
		});
	}

	[Test]
	public async Task CallerRollback_RevertsPersistedRowsWithoutReceipt()
	{
		SyncBatch batch = Batch();
		batch.Added.Add(Catalog("category", Guid.NewGuid(), "Rollback", RootsIds.CategoryGroupId));
		using (IServiceScope scope = _provider.CreateScope())
		{
			IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
			IUnitOfWorkTransaction transaction = await unit.BeginTransaction();
			await scope.ServiceProvider.GetRequiredService<IApplySyncBatchOperation>().Run(batch, _local, 11);
			Assert.That(await unit.CategoryRepo.GetAll(), Has.Count.EqualTo(1));
			Assert.That((await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(10));
			Assert.That(await unit.LocalSyncBatchRepo.GetAll(), Has.Count.EqualTo(2));
			await unit.RollbackTransaction(transaction);
		}
		await WithUnit(async unit => Assert.That(await unit.CategoryRepo.GetAll(), Is.Empty));
	}

	[Test]
	public async Task CumulativeOverflow_RollsBackAlreadySavedAggregates()
	{
		Guid accountId = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			unit.AccountRepo.Add(new Account { Id = accountId, Name = "Other", GroupId = RootsIds.AccountGroupId, CurrencyId = _baseCurrency });
			await unit.SaveChanges();
		});
		SyncBatch batch = Batch();
		for (int index = 0; index < 2; index++)
		{
			batch.Added.Add(TransactionObject(Guid.NewGuid(), null,
				[Entry(EntitiesIds.BalancingAccountId, 600000000000000m, 0), Entry(accountId, -600000000000000m, 1)]));
		}
		Assert.ThrowsAsync<SqliteException>(async () => await Apply(batch));
		await WithUnit(async unit =>
		{
			Assert.That(await unit.TransactionRepo.GetAll(), Is.Empty);
			Assert.That(await unit.TransactionEntryRepo.GetAll(), Is.Empty);
			Assert.That((await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(10));
		});
	}

	[Test]
	public void CancelledBatch_StopsBeforeWrites()
	{
		using IServiceScope scope = _provider.CreateScope();
		using CancellationTokenSource source = new();
		source.Cancel();
		Assert.ThrowsAsync<OperationCanceledException>(async () => await scope.ServiceProvider.GetRequiredService<IApplySyncBatchOperation>().Run(Batch(), _local, 11, source.Token));
	}

	[Test]
	public async Task ProtectedRows_RejectDeletionAndRootChanges()
	{
		SyncBatch batch = Batch();
		SyncObject currency = CurrencyObject(_baseCurrency, "UAH", "UAH");
		currency.EditRevision = 0;
		currency.ModificationType = ModificationType.Content;
		batch.Deleted.Add(currency);
		SyncObject account = AccountObject(EntitiesIds.BalancingAccountId, RootsIds.AccountGroupId, _baseCurrency);
		account.EditRevision = 0;
		account.ModificationType = ModificationType.Content;
		batch.Deleted.Add(account);
		Guid initialId = Guid.Empty;
		await WithUnit(async unit => initialId = (await unit.CurrencyRateRepo.GetAll()).Single().Id);
		batch.Deleted.Add(RateObject(initialId, _baseCurrency, AppValues.InitialDate, 1m, 0));
		SyncObject root = Catalog("accountGroup", RootsIds.AccountGroupId, "Changed root", Guid.NewGuid(), 0);
		root.Properties["isFavorite"] = JsonSerializer.SerializeToElement(true);
		batch.Updated.Add(root);
		await Apply(batch);
		await WithUnit(async unit =>
		{
			AccountGroup storedRoot = (await unit.AccountGroupRepo.GetById(RootsIds.AccountGroupId))!;
			Assert.That((storedRoot.Name, storedRoot.ParentId, storedRoot.IsFavorite, storedRoot.DeleteRevision),
				Is.EqualTo(("Account", RootsIds.AccountGroupId, false, (long?)null)));
			Assert.That((await unit.AccountRepo.GetById(EntitiesIds.BalancingAccountId))!.DeleteRevision, Is.Null);
			Assert.That((await unit.CurrencyRepo.GetById(_baseCurrency))!.DeleteRevision, Is.Null);
			Assert.That((await unit.CurrencyRateRepo.GetById(initialId))!.DeleteRevision, Is.Null);
		});
	}

	[Test]
	public async Task AccountCurrencyChange_PreservesOriginalCurrency()
	{
		Guid foreign = await SeedForeignCurrency();
		SyncObject item = AccountObject(EntitiesIds.BalancingAccountId, RootsIds.AccountGroupId, foreign);
		item.EditRevision = 0;
		item.ModificationType = ModificationType.Content;
		item.Properties.Remove("order");
		SyncBatch batch = Batch();
		batch.Updated.Add(item);
		await Apply(batch);
		await WithUnit(async unit => Assert.That((await unit.AccountRepo.GetById(EntitiesIds.BalancingAccountId))!.CurrencyId, Is.EqualTo(_baseCurrency)));
	}

	[Test]
	public async Task OrderTie_UsesCanonicalGuidAndSeparateGroupElementSequences()
	{
		Guid lower = Guid.Parse("00000001-0000-0000-0000-000000000000");
		Guid higher = Guid.Parse("01000000-0000-0000-0000-000000000000");
		SyncBatch batch = Batch();
		batch.Added.Add(Catalog("category", higher, "Higher", RootsIds.CategoryGroupId));
		batch.Added.Add(Catalog("category", lower, "Lower", RootsIds.CategoryGroupId));
		Guid group = Guid.NewGuid();
		batch.Added.Add(Catalog("categoryGroup", group, "Group", RootsIds.CategoryGroupId));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.CategoryRepo.GetById(lower))!.Order, Is.EqualTo(0));
			Assert.That((await unit.CategoryRepo.GetById(higher))!.Order, Is.EqualTo(1));
			Assert.That((await unit.CategoryGroupRepo.GetById(group))!.Order, Is.EqualTo(0));
		});
	}

	[Test]
	public async Task NameCorrection_DoesNotStealAnAlreadyUniqueSuffixedName()
	{
		await SeedCategory("Name");
		Guid suffixed = await SeedCategory("Name_1");
		Guid added = Guid.NewGuid();
		SyncBatch batch = Batch(ConflictPriority.Master);
		batch.Added.Add(Catalog("category", added, "Name", RootsIds.CategoryGroupId));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.CategoryRepo.GetById(suffixed))!.Name, Is.EqualTo("Name_1"));
			Assert.That((await unit.CategoryRepo.GetById(added))!.Name, Is.EqualTo("Name_1_1"));
		});
	}

	[Test]
	public async Task EmptyBatch_LeavesEntityRevisionsUnchanged()
	{
		Guid id = await SeedCategory();
		await Apply(Batch());
		await WithUnit(async unit => Assert.That((await unit.CategoryRepo.GetById(id))!.EditRevision, Is.EqualTo(10)));
	}

	[TestCase("missingAccount")]
	[TestCase("negativeRate")]
	[TestCase("baseRate")]
	[TestCase("amountOverflow")]
	[TestCase("badPosition")]
	[TestCase("duplicateEntry")]
	[TestCase("invalidState")]
	[TestCase("localDate")]
	public async Task InvalidAggregate_RejectsWholeBatch(string scenario)
	{
		Dictionary<string, object> first = Entry(EntitiesIds.BalancingAccountId, 1m, 0);
		Dictionary<string, object> second = Entry(EntitiesIds.BalancingAccountId, -1m, 1);
		switch (scenario)
		{
			case "missingAccount": first["accountId"] = Guid.NewGuid(); break;
			case "negativeRate": first["rate"] = -1m; break;
			case "baseRate": first["rate"] = 2m; break;
			case "amountOverflow": first["amount"] = decimal.MaxValue; break;
			case "badPosition": first["position"] = 1; break;
			case "duplicateEntry": second["id"] = first["id"]; break;
		}
		SyncObject item = TransactionObject(Guid.NewGuid(), null, [first, second]);
		if (scenario == "invalidState") { item.Properties["state"] = JsonSerializer.SerializeToElement(9); }
		if (scenario == "localDate") { item.Properties["dateTime"] = JsonSerializer.SerializeToElement("2026-10-20T12:00:00"); }
		SyncBatch batch = Batch();
		batch.Added.Add(Catalog("category", Guid.NewGuid(), "Valid", RootsIds.CategoryGroupId));
		batch.Added.Add(item);
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Apply(batch));
		await WithUnit(async unit =>
		{
			Assert.That(await unit.CategoryRepo.GetAll(), Is.Empty);
			Assert.That(await unit.TransactionRepo.GetAll(), Is.Empty);
		});
	}

	[Test]
	public async Task DuplicateRatePair_IsRejectedEvenWithDifferentIds()
	{
		SyncBatch batch = Batch();
		batch.Added.Add(RateObject(Guid.NewGuid(), _baseCurrency, new DateOnly(2026, 10, 20), 1m));
		batch.Added.Add(RateObject(Guid.NewGuid(), _baseCurrency, new DateOnly(2026, 10, 20), 1m));
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Apply(batch));
		await WithUnit(async unit => Assert.That(await unit.CurrencyRateRepo.GetAll(), Has.Count.EqualTo(1)));
	}

	[Test]
	public async Task ExistingRate_CannotChangeCurrencyDateKey()
	{
		Guid initial = Guid.Empty;
		await WithUnit(async unit => initial = (await unit.CurrencyRateRepo.GetAll()).Single().Id);
		SyncBatch batch = Batch();
		batch.Updated.Add(RateObject(initial, _baseCurrency, new DateOnly(2026, 10, 20), 1m, 0));
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Apply(batch));
	}

	[Test]
	public async Task ExistingReceipt_MustBeHandledByCallerWithoutReapplying()
	{
		SyncBatch batch = Batch();
		batch.SyncKey = "previous-one";
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Apply(batch));
		await WithUnit(async unit => Assert.That((await unit.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(10)));
	}

	[Test]
	public async Task LosingTransactionContent_KeepsEntireMasterEntrySet()
	{
		Guid id = Guid.NewGuid();
		SyncBatch first = Batch();
		first.Added.Add(TransactionObject(id, null, [Entry(EntitiesIds.BalancingAccountId, 1m, 0), Entry(EntitiesIds.BalancingAccountId, -1m, 1)]));
		await Apply(first);
		await WithUnit(async unit =>
		{
			LocalSyncBatch receipt = (await unit.LocalSyncBatchRepo.GetAll()).Single(value => value.Revision == 11);
			receipt.LocalRegistrationId = _other;
			unit.LocalSyncBatchRepo.Update(receipt);
			await unit.SaveChanges();
		});
		SyncBatch second = Batch(ConflictPriority.Master);
		second.Updated.Add(TransactionObject(id, 0, [Entry(EntitiesIds.BalancingAccountId, 10m, 0)]));
		await Apply(second, 12);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.TransactionEntryRepo.GetByTransactionId(id)).Select(entry => entry.Amount), Is.EquivalentTo((decimal[])[1m, -1m]));
			Assert.That((await unit.TransactionRepo.GetById(id))!.EditRevision, Is.EqualTo(11));
			Assert.That((await unit.TransactionRepo.GetById(id))!.DateTime.Kind, Is.EqualTo(DateTimeKind.Utc));
		});
	}

	[Test]
	public async Task SequentialBatches_UpdateRetainedEntryIdentities()
	{
		Guid id = Guid.NewGuid();
		Dictionary<string, object> firstEntry = Entry(EntitiesIds.BalancingAccountId, 2m, 0);
		Dictionary<string, object> secondEntry = Entry(EntitiesIds.BalancingAccountId, -2m, 1);
		SyncBatch first = Batch();
		first.Added.Add(TransactionObject(id, null, [firstEntry, secondEntry]));
		firstEntry["amount"] = 4m;
		secondEntry["amount"] = -4m;
		SyncBatch second = Batch();
		second.Updated.Add(TransactionObject(id, 11, [firstEntry, secondEntry]));
		using (IServiceScope scope = _provider.CreateScope())
		{
			await ApplyInScope(scope, first, 11);
			await ApplyInScope(scope, second, 12);
		}
		await WithUnit(async unit =>
		{
			ICollection<TransactionEntry> entries = await unit.TransactionEntryRepo.GetByTransactionId(id);
			Assert.That(entries.Select(entry => entry.Id), Is.EquivalentTo(new[] { (Guid)firstEntry["id"], (Guid)secondEntry["id"] }));
			Assert.That(entries.Select(entry => entry.Amount), Is.EquivalentTo((decimal[])[4m, -4m]));
			Assert.That(entries.Select(entry => entry.CumulativeAmount), Is.EquivalentTo((decimal[])[4m, 0m]));
		});
	}

	[Test]
	public async Task EntryCannotMoveBetweenAggregates()
	{
		Guid existingId = Guid.NewGuid();
		Dictionary<string, object> entry = Entry(EntitiesIds.BalancingAccountId, 2m, 0);
		SyncBatch first = Batch();
		first.Added.Add(TransactionObject(existingId, null, [entry]));
		await Apply(first);
		SyncBatch next = Batch();
		next.Updated.Add(TransactionObject(existingId, 11, []));
		next.Added.Add(TransactionObject(Guid.NewGuid(), null, [entry]));
		Assert.ThrowsAsync<InvalidSyncBatchException>(async () => await Apply(next, 12));
		await WithUnit(async unit =>
		{
			Assert.That(await unit.TransactionRepo.GetAll(), Has.Count.EqualTo(1));
			Assert.That(await unit.TransactionEntryRepo.GetByTransactionId(existingId), Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task NewElementUnderDeletedGroup_RestoresOriginalGroup()
	{
		Guid groupId = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			unit.CategoryGroupRepo.Add(new CategoryGroup { Id = groupId, Name = "Original", ParentId = RootsIds.CategoryGroupId, DeleteRevision = 10, EditRevision = 10 });
			await unit.SaveChanges();
		});
		Guid id = Guid.NewGuid();
		SyncBatch batch = Batch(ConflictPriority.Master);
		batch.Added.Add(Catalog("category", id, "Offline", groupId));
		await Apply(batch);
		await WithUnit(async unit =>
		{
			Assert.That((await unit.CategoryGroupRepo.GetById(groupId))!.DeleteRevision, Is.Null);
			Assert.That((await unit.CategoryRepo.GetById(id))!.GroupId, Is.EqualTo(groupId));
		});
	}

	private async Task<Guid> SeedCategory(string name = "Master", int order = 0, long revision = 10)
	{
		Guid id = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			unit.CategoryRepo.Add(new Category { Id = id, GroupId = RootsIds.CategoryGroupId, Name = name, Order = order, EditRevision = revision });
			await unit.SaveChanges();
		});
		return id;
	}

	private async Task<Guid> SeedForeignCurrency()
	{
		Guid id = Guid.NewGuid();
		await WithUnit(async unit =>
		{
			unit.CurrencyRepo.Add(new Currency { Id = id, Code = "EUR", Name = "EUR", EnglishName = "Euro", Symbol = "E", EditRevision = 10, Order = 1 });
			unit.CurrencyRateRepo.Add(new CurrencyRate { Id = Guid.NewGuid(), CurrencyId = id, Currency = null!, Date = AppValues.InitialDate, Rate = 1m, EditRevision = 10 });
			await unit.SaveChanges();
		});
		return id;
	}

	private async Task WithUnit(Func<IMasterAppUnitOfWork, Task> action)
	{
		using IServiceScope scope = _provider.CreateScope();
		await action(scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>());
	}

	private async Task Apply(SyncBatch batch, long revision = 11)
	{
		using IServiceScope scope = _provider.CreateScope();
		await ApplyInScope(scope, batch, revision);
	}

	private async Task ApplyInScope(IServiceScope scope, SyncBatch batch, long revision)
	{
		IMasterAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IMasterAppUnitOfWork>();
		IUnitOfWorkTransaction transaction = await unit.BeginTransaction();
		try
		{
			await scope.ServiceProvider.GetRequiredService<IApplySyncBatchOperation>().Run(batch, _local, revision);
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision = revision;
			unit.SystemConfigRepo.Update(config);
			unit.LocalSyncBatchRepo.Add(Receipt(batch.SyncKey, revision, _local));
			await unit.SaveChanges();
			await unit.CommitTransaction(transaction);
		}
		catch
		{
			await unit.RollbackTransaction(transaction);
			throw;
		}
	}

	private static LocalSyncBatch Receipt(string key, long revision, Guid local) => new()
	{
		Id = Guid.NewGuid(), LocalRegistrationId = local, SyncKey = key, Revision = revision, PayloadHash = "test"
	};

	private static SyncBatch Batch(ConflictPriority priority = ConflictPriority.Local) => new()
	{
		SyncKey = Guid.NewGuid().ToString("N"), CreatedAt = DateTime.UtcNow, Priority = priority
	};

	private static SyncObject Object(string type, Guid id, long? revision, ModificationType flags, params (string Name, object? Value)[] fields)
	{
		SyncObject item = new() { Type = type, Id = id, EditRevision = revision, ModificationType = flags };
		foreach ((string name, object? value) in fields) { item.Properties.Add(name, JsonSerializer.SerializeToElement(value, _json)); }
		return item;
	}

	private static SyncObject Catalog(string type, Guid id, string name, Guid parent, long? revision = null, int order = 0, ModificationType flags = ModificationType.Content)
	{
		SyncObject item = Object(type, id, revision, revision is null ? ModificationType.None : flags,
			("name", name), ("description", null), ("isFavorite", false), (type.EndsWith("Group", StringComparison.Ordinal) ? "parentId" : "groupId", parent));
		if (revision is null || flags.HasFlag(ModificationType.Order)) { item.Properties.Add("order", JsonSerializer.SerializeToElement(order)); }
		return item;
	}

	private static SyncObject CurrencyObject(Guid id, string code, string name) => Object("currency", id, null, ModificationType.None,
		("code", code), ("name", name), ("symbol", code), ("englishName", code), ("isFavorite", false), ("order", 1));

	private static SyncObject RateObject(Guid id, Guid currency, DateOnly date, decimal rate, long? revision = null) => Object("currencyRate", id, revision,
		revision is null ? ModificationType.None : ModificationType.Content, ("currencyId", currency), ("date", date), ("rate", rate), ("description", null));

	private static SyncObject AccountObject(Guid id, Guid group, Guid currency)
	{
		SyncObject item = Catalog("account", id, "Account", group);
		item.Properties.Add("currencyId", JsonSerializer.SerializeToElement(currency));
		foreach (string name in new[] { "categoryId", "correspondentId", "projectId" }) { item.Properties.Add(name, JsonSerializer.SerializeToElement((Guid?)null)); }
		return item;
	}

	private static Dictionary<string, object> Entry(Guid account, decimal amount, int position) => new()
	{
		["id"] = Guid.NewGuid(), ["accountId"] = account, ["position"] = position, ["amount"] = amount, ["rate"] = 1m
	};

	private static SyncObject TransactionObject(Guid id, long? revision, Dictionary<string, object>[] entries) => Object("transaction", id, revision,
		revision is null ? ModificationType.None : ModificationType.Content, ("dateTime", new DateTime(2026, 10, 20, 12, 0, 0, DateTimeKind.Utc)),
		("state", TransactionState.Confirmed), ("description", "Test"), ("entries", entries));
}
