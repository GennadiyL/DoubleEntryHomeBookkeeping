using Business.Models.Entities.Synchronizations;
using Business.Contracts.Services;
using Business.Contracts.Utils.Models;
using Business.Impl;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Reporting;
using Business.Models.Enums;
using DataAccess.Contracts;
using DataAccess.EntityFramework;
using DataAccess.EntityFramework.SqLite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace UnitTests.EntityFramework.SqLite;

/// <summary>
/// Verifies the fresh SQLite scripts against the current persistence model.
/// Uses the production DAL and isolated in-memory databases for every test.
/// Initial and optional sample data must load with foreign keys enabled.
/// Report groups and reports round-trip through detached repository calls.
/// Hierarchy reads preserve loaded parent, child and element relationships.
/// Local naming settings survive writes and are used by Business services.
/// Schema columns are checked against EF metadata to detect script drift.
/// No existing user database is opened, migrated or replaced.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the database lifetime connection after each test.")]
[TestFixture(Category = "Local")]
public sealed class CatalogSchemaTests
{
	private ServiceProvider _provider = null!;
	private SqliteConnection _lifetime = null!;

	[SetUp]
	public async Task SetUp()
	{
		string connectionString = $"Data Source=catalog-{Guid.NewGuid()};Mode=Memory;Cache=Shared;Pooling=False";
		_lifetime = new SqliteConnection(connectionString);
		await _lifetime.OpenAsync();
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddSharedMockModule();
		services.AddBusinessModule();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
			new Dictionary<string, string?> { ["ConnectionStrings:DehbSqLite"] = connectionString }).Build();
		services.AddDataAccessSqLiteModule(configuration);
		_provider = services.BuildServiceProvider(true);
		foreach (string file in new[] { "001-schema.sql", "002-initial-data.sql", "003-test-data.sql" })
		{
			await using SqliteCommand command = _lifetime.CreateCommand();
			command.CommandText = await File.ReadAllTextAsync(Path.Combine(TestContext.CurrentContext.TestDirectory, "SqlAssets", file));
			await command.ExecuteNonQueryAsync();
		}
	}

	[TearDown]
	public void TearDown()
	{
		_provider?.Dispose();
		_lifetime?.Dispose();
	}

	[Test]
	public async Task Scripts_MatchEfColumnsAndSeedAllRoots()
	{
		using IServiceScope scope = _provider.CreateScope();
		AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		foreach (ITable table in context.Model.GetRelationalModel().Tables)
		{
			await using SqliteCommand command = _lifetime.CreateCommand();
			command.CommandText = $"PRAGMA table_info(\"{table.Name}\");";
			await using SqliteDataReader reader = await command.ExecuteReaderAsync();
			Dictionary<string, (string Type, bool Nullable)> columns = new();
			while (await reader.ReadAsync())
			{
				columns.Add(reader.GetString(1), (reader.GetString(2), reader.GetInt32(3) == 0));
			}
			Assert.That(columns.Keys, Is.EquivalentTo(table.Columns.Select(column => column.Name)), table.Name);
			foreach (IColumn column in table.Columns)
			{
				Assert.That(columns[column.Name], Is.EqualTo((column.StoreType, column.IsNullable)), $"{table.Name}.{column.Name}");
			}
		}
		IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		Assert.Multiple(() =>
		{
			Assert.That(Roots.IsRoot(RootsIds.ReportGroupId), Is.True);
			Assert.That(Roots.ReportGroup.Parent, Is.SameAs(Roots.ReportGroup));
			Assert.That(Roots.ReportGroup.Children, Is.Empty);
		});
		Assert.That((await unit.AccountGroupRepo.GetById(RootsIds.AccountGroupId))!.ParentId, Is.EqualTo(RootsIds.AccountGroupId));
		Assert.That((await unit.CategoryGroupRepo.GetById(RootsIds.CategoryGroupId))!.ParentId, Is.EqualTo(RootsIds.CategoryGroupId));
		Assert.That((await unit.CorrespondentGroupRepo.GetById(RootsIds.CorrespondentGroupId))!.ParentId, Is.EqualTo(RootsIds.CorrespondentGroupId));
		Assert.That((await unit.ProjectGroupRepo.GetById(RootsIds.ProjectGroupId))!.ParentId, Is.EqualTo(RootsIds.ProjectGroupId));
		Assert.That((await unit.TemplateGroupRepo.GetById(RootsIds.TemplateGroupId))!.ParentId, Is.EqualTo(RootsIds.TemplateGroupId));
		Assert.That((await unit.ReportGroupRepo.GetById(RootsIds.ReportGroupId))!.ParentId, Is.EqualTo(RootsIds.ReportGroupId));
		await using SqliteCommand check = _lifetime.CreateCommand();
		check.CommandText = "PRAGMA foreign_key_check;";
		Assert.That(await check.ExecuteScalarAsync(), Is.Null);
		check.CommandText = "PRAGMA user_version;";
		Assert.That(await check.ExecuteScalarAsync(), Is.EqualTo(2L));
	}

	[Test]
	public async Task Scripts_StoreGuidValuesAsSixteenByteBlobs()
	{
		using IServiceScope scope = _provider.CreateScope();
		AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		foreach (IEntityType entityType in context.Model.GetEntityTypes())
		{
			StoreObjectIdentifier table = StoreObjectIdentifier.Table(entityType.GetTableName()!, entityType.GetSchema());
			foreach (IProperty property in entityType.GetProperties())
			{
				if ((Nullable.GetUnderlyingType(property.ClrType) ?? property.ClrType) != typeof(Guid))
				{
					continue;
				}
				string column = property.GetColumnName(table)!;
				Assert.That(property.GetColumnType(), Is.EqualTo("BLOB"), $"{table.Name}.{column}");
				await using SqliteCommand command = _lifetime.CreateCommand();
				command.CommandText = $"SELECT COUNT(*) FROM \"{table.Name}\" WHERE \"{column}\" IS NOT NULL AND (typeof(\"{column}\") <> 'blob' OR length(\"{column}\") <> 16);";
				Assert.That(await command.ExecuteScalarAsync(), Is.EqualTo(0L), $"{table.Name}.{column}");
			}
		}
		IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		Account? account = await unit.AccountRepo.GetById(Guid.Parse("00000000-0000-0000-0000-000000000012"));
		Assert.That(account!.CategoryId, Is.EqualTo(Guid.Parse("00000000-0000-0000-0000-000000000020")));
		Account? cash = await unit.AccountRepo.GetById(Guid.Parse("00000000-0000-0000-0000-000000000011"));
		Assert.That(cash!.CategoryId, Is.Null);
	}

	[Test]
	public async Task Currency_RoundTripsCatalogMetadataAndNameOnlyUpdate()
	{
		Guid id;
		string englishName;
		string symbol;
		using (IServiceScope scope = _provider.CreateScope())
		{
			ICurrencyService service = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
			var profile = (await service.GetAvailableCurrencies()).Single(item => item.Code == "EUR");
			englishName = profile.EnglishName;
			symbol = profile.Symbol;
			id = await service.Add("EUR", 40m);
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			ICurrencyService service = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
			var currency = await service.GetById(id);
			Assert.That((currency.Code, currency.Name, currency.EnglishName, currency.Symbol), Is.EqualTo(("EUR", "EUR", englishName, symbol)));
			await service.Update(id, new Business.Contracts.Services.Currencies.CurrencyParam { Name = "Euro" });
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			ICurrencyService service = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
			var currency = await service.GetById(id);
			Assert.That((currency.Code, currency.Name, currency.EnglishName, currency.Symbol), Is.EqualTo(("EUR", "Euro", englishName, symbol)));
			var rates = await scope.ServiceProvider.GetRequiredService<ICurrencyRateService>().GetRates(id);
			Assert.That(rates.Single().Rate, Is.EqualTo(40m));
		}
	}

	[Test]
	public async Task Currency_AddDeleteAdd_PreservesDeletedRecordAndCreatesNewInitialRate()
	{
		Guid deletedId;
		Guid newId;
		using (IServiceScope scope = _provider.CreateScope())
		{
			deletedId = await scope.ServiceProvider.GetRequiredService<ICurrencyService>().Add("EUR", 40m);
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			await scope.ServiceProvider.GetRequiredService<ICurrencyService>().Delete(deletedId);
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			newId = await scope.ServiceProvider.GetRequiredService<ICurrencyService>().Add("EUR", 54.2654m);
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			ICurrencyService service = scope.ServiceProvider.GetRequiredService<ICurrencyService>();
			Assert.That(newId, Is.Not.EqualTo(deletedId));
			Assert.That((await service.GetAllCurrencies()).Single(currency => currency.Code == "EUR").Id, Is.EqualTo(newId));
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			Assert.That((await unit.CurrencyRepo.GetById(deletedId))!.DeleteRevision, Is.EqualTo(0));
			var rates = await scope.ServiceProvider.GetRequiredService<ICurrencyRateService>().GetRates(newId);
			Assert.That(rates.Single().Rate, Is.EqualTo(54.2654m));
			Assert.That(rates.Single().IsInitial, Is.True);
			Assert.ThrowsAsync<Business.Models.Exceptions.InvalidCurrencyException>(async () => await service.Add("EUR", 1m));
		}
	}

	[Test]
	public async Task Reports_RoundTripHierarchyAndUpdateInSeparateScope()
	{
		Guid groupId = Guid.NewGuid();
		Guid childId = Guid.NewGuid();
		Guid reportId = Guid.NewGuid();
		const string instructions = "{\"uninterpreted\":true}";
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			unit.ReportGroupRepo.Add(new ReportGroup { Id = groupId, ParentId = RootsIds.ReportGroupId, Name = "Monthly", IsFavorite = true, Order = 3 });
			unit.ReportGroupRepo.Add(new ReportGroup { Id = childId, ParentId = groupId, Name = "Child" });
			unit.ReportRepo.Add(new Report { Id = reportId, GroupId = groupId, Name = "Spending", Json = instructions, IsFavorite = true, Order = 2, EditRevision = 7 });
			await unit.SaveChanges();
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			ReportGroup group = (await unit.ReportGroupRepo.GetWithContentsById(groupId))!;
			Assert.That(group.Children.Single().Id, Is.EqualTo(childId));
			Assert.That(group.Children.Single().Parent, Is.SameAs(group));
			Report report = group.Elements.Single();
			Assert.That(report.Group, Is.SameAs(group));
			Assert.That((report.Id, report.Json, report.IsFavorite, report.Order, report.EditRevision), Is.EqualTo((reportId, instructions, true, 2, 7L)));
			Assert.That((group.ParentId, group.IsFavorite, group.Order), Is.EqualTo((RootsIds.ReportGroupId, true, 3)));
			report.GroupId = RootsIds.ReportGroupId;
			report.Name = "Renamed";
			report.IsFavorite = false;
			report.Order = 4;
			report.ModificationType = ModificationType.Content | ModificationType.Order;
			unit.ReportRepo.Update(report);
			await unit.SaveChanges();
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			Report report = (await unit.ReportRepo.GetById(reportId))!;
			Assert.That((report.GroupId, report.Name, report.Json, report.IsFavorite, report.Order), Is.EqualTo((RootsIds.ReportGroupId, "Renamed", instructions, false, 4)));
			Assert.That((await unit.ReportGroupRepo.GetWithContentsById(groupId))!.Elements, Is.Empty);
		}
	}

	[Test]
	public async Task LocalNamingSettings_RoundTripAndDriveBusinessNameGeneration()
	{
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			LocalConfig config = (await unit.LocalConfigRepo.GetAll()).Single();
			Assert.That(config.AccountNameAddCurrency, Is.False);
			Assert.That(config.AccountNameSeparator, Is.EqualTo("/"));
			config.AccountNameSeparator = "|";
			config.AccountNameOrder = AccountNameOrder.ProjectCategoryCorrespondent;
			config.AccountNameAddCurrency = true;
			unit.LocalConfigRepo.Update(config);
			await unit.SaveChanges();
		}
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			LocalConfig config = (await unit.LocalConfigRepo.GetAll()).Single();
			Assert.That((config.AccountNameSeparator, config.AccountNameOrder, config.AccountNameAddCurrency), Is.EqualTo(("|", AccountNameOrder.ProjectCategoryCorrespondent, true)));
			Guid baseCurrencyId = (await unit.SystemConfigRepo.GetAll()).Single().BaseCurrencyId;
			IAccountService accounts = scope.ServiceProvider.GetRequiredService<IAccountService>();
			Assert.That(await accounts.GetDefaultName(null, null, null, baseCurrencyId), Is.EqualTo("||(UAH)"));
		}
	}

	[TestCase(null, null, ModificationType.None, true)]
	[TestCase(null, 0L, ModificationType.Content, true)]
	[TestCase(0L, null, ModificationType.None, false)]
	[TestCase(5L, null, ModificationType.None, false)]
	[TestCase(5L, null, ModificationType.Content, true)]
	[TestCase(5L, null, ModificationType.Order, true)]
	[TestCase(5L, null, ModificationType.Content | ModificationType.Order, true)]
	[TestCase(0L, 0L, ModificationType.Content, true)]
	[TestCase(5L, 0L, ModificationType.Content, true)]
	[TestCase(5L, 0L, ModificationType.None, false)]
	[TestCase(5L, 5L, ModificationType.None, false)]
	public async Task GetChanges_SelectsUncapturedRowsIncludingDeletions(long? editRevision, long? deleteRevision, ModificationType flags, bool expected)
	{
		using IServiceScope scope = _provider.CreateScope();
		IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		Currency currency = new()
		{
			Id = Guid.NewGuid(), Code = "TST", Symbol = "T", EnglishName = "Test", Name = "Test",
			EditRevision = editRevision, DeleteRevision = deleteRevision, ModificationType = flags
		};
		unit.CurrencyRepo.Add(currency);
		await unit.SaveChanges();
		AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		context.ChangeTracker.Clear();
		ICollection<Currency> changes = await unit.CurrencyRepo.GetChanges();
		Assert.That(changes.Any(item => item.Id == currency.Id), Is.EqualTo(expected));
		Assert.That(context.ChangeTracker.Entries(), Is.Empty);
		Currency persisted = (await unit.CurrencyRepo.GetById(currency.Id))!;
		Assert.Multiple(() =>
		{
			Assert.That(persisted.EditRevision, Is.EqualTo(editRevision));
			Assert.That(persisted.DeleteRevision, Is.EqualTo(deleteRevision));
			Assert.That(persisted.ModificationType, Is.EqualTo(flags));
		});
	}

	[Test]
	public async Task GetChanges_LoadsEntriesForSoftDeletedAggregates()
	{
		await using SqliteCommand command = _lifetime.CreateCommand();
		command.CommandText = "UPDATE \"Transaction\" SET EditRevision = 5, DeleteRevision = 0, ModificationType = 1; UPDATE \"Template\" SET EditRevision = 5, DeleteRevision = 0, ModificationType = 1;";
		await command.ExecuteNonQueryAsync();
		using IServiceScope scope = _provider.CreateScope();
		IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		ICollection<Transaction> transactions = await unit.TransactionRepo.GetChanges();
		ICollection<Template> templates = await unit.TemplateRepo.GetChanges();
		Assert.Multiple(() =>
		{
			Assert.That(transactions, Has.Count.EqualTo(2));
			Assert.That(transactions.Sum(item => item.Entries.Count), Is.EqualTo(4));
			Assert.That(templates, Has.Count.EqualTo(1));
			Assert.That(templates.Single().Entries, Has.Count.EqualTo(2));
			Assert.That(transactions.All(item => item.DeleteRevision == 0), Is.True);
			Assert.That(templates.Single().DeleteRevision, Is.Zero);
		});
		foreach (Transaction transaction in transactions)
		{
			Assert.That(transaction.Entries.Select(entry => entry.Position), Is.Ordered);
		}
		Assert.That(templates.Single().Entries.Select(entry => entry.Position), Is.Ordered);
		Assert.That(scope.ServiceProvider.GetRequiredService<AppDbContext>().ChangeTracker.Entries(), Is.Empty);
	}

	[Test]
	public async Task AcceptChanges_BulkClearsFlagsAndPreservesKnownRevisions()
	{
		using IServiceScope scope = _provider.CreateScope();
		IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		await unit.CurrencyRepo.AcceptChanges();
		Currency created = new() { Id = Guid.NewGuid(), Code = "TS1", Symbol = "T", EnglishName = "Test", Name = "New" };
		Currency edited = new() { Id = Guid.NewGuid(), Code = "TS2", Symbol = "T", EnglishName = "Test", Name = "Edited", EditRevision = 7, ModificationType = ModificationType.Content | ModificationType.Order };
		Currency captured = new() { Id = Guid.NewGuid(), Code = "TS3", Symbol = "T", EnglishName = "Test", Name = "Captured", EditRevision = 0 };
		unit.CurrencyRepo.Add(created);
		unit.CurrencyRepo.Add(edited);
		unit.CurrencyRepo.Add(captured);
		await unit.SaveChanges();
		AppDbContext context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
		context.ChangeTracker.Clear();
		int count = await unit.CurrencyRepo.AcceptChanges();
		Currency first = (await unit.CurrencyRepo.GetById(created.Id))!;
		Currency second = (await unit.CurrencyRepo.GetById(edited.Id))!;
		Assert.Multiple(() =>
		{
			Assert.That(count, Is.EqualTo(2));
			Assert.That(first.EditRevision, Is.Zero);
			Assert.That(second.EditRevision, Is.EqualTo(7));
			Assert.That(second.ModificationType, Is.EqualTo(ModificationType.None));
			Assert.That(second.Name, Is.EqualTo("Edited"));
			Assert.That(second.DeleteRevision, Is.Null);
			Assert.That(context.ChangeTracker.Entries(), Is.Empty);
		});
		Assert.That(await unit.CurrencyRepo.AcceptChanges(), Is.Zero);
	}

	[Test]
	public async Task DeleteSoftDeleted_RemovesOnlyLocalMarkersAndCascadesOwnedEntries()
	{
		await using SqliteCommand command = _lifetime.CreateCommand();
		command.CommandText = "UPDATE \"Transaction\" SET DeleteRevision = 0 WHERE Description = 'Opening cash'; UPDATE \"Transaction\" SET DeleteRevision = 8 WHERE Description = 'Groceries'; UPDATE \"Template\" SET DeleteRevision = 0;";
		await command.ExecuteNonQueryAsync();
		using IServiceScope scope = _provider.CreateScope();
		IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		Assert.That(await unit.TransactionRepo.HardDelete(), Is.EqualTo(1));
		Assert.That(await unit.TemplateRepo.HardDelete(), Is.EqualTo(1));
		Assert.That((await unit.TransactionRepo.GetAll()).Single().DeleteRevision, Is.EqualTo(8));
		Assert.That(await unit.TemplateRepo.GetAll(), Is.Empty);
		Assert.That(await unit.TransactionEntryRepo.GetAll(), Has.Count.EqualTo(2));
		Assert.That(await unit.TemplateEntryRepo.GetAll(), Is.Empty);
		Assert.That(await unit.TransactionRepo.HardDelete(), Is.Zero);
		Assert.That(await unit.AccountRepo.HardDelete(), Is.Zero);
	}

	[Test]
	public async Task BulkAcceptance_RollsBackWithCallerTransaction()
	{
		await using SqliteCommand command = _lifetime.CreateCommand();
		command.CommandText = "UPDATE \"Template\" SET DeleteRevision = 0;";
		await command.ExecuteNonQueryAsync();
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			DataAccess.Core.Entities.IUnitOfWorkTransaction transaction = await unit.BeginTransaction();
			await unit.TemplateRepo.HardDelete();
			await unit.CurrencyRepo.AcceptChanges();
			Assert.That(await unit.TemplateRepo.GetAll(), Is.Empty);
			Assert.That(await unit.CurrencyRepo.GetChanges(), Is.Empty);
			await unit.RollbackTransaction(transaction);
		}
		using IServiceScope verify = _provider.CreateScope();
		IAppUnitOfWork restored = verify.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		Assert.That(await restored.TemplateRepo.GetAll(), Has.Count.EqualTo(1));
		Assert.That(await restored.TemplateEntryRepo.GetAll(), Has.Count.EqualTo(2));
		Assert.That(await restored.CurrencyRepo.GetChanges(), Is.Not.Empty);
	}
	[Test]
	public async Task SynchronizationMetadata_RoundTripsAndLoadsRegistration()
	{
		LocalRegistration registration = new()
		{
			Id = Guid.NewGuid(), LocalDatasetKey = "device-a",
			SyncDateTime = new DateTime(2026, 10, 21, 12, 30, 0, DateTimeKind.Utc), SyncRevision = 8
		};
		LocalSyncBatch batch = new()
		{
			Id = Guid.NewGuid(), LocalRegistrationId = registration.Id, LocalRegistration = registration,
			SyncKey = "batch-a", Revision = 9, PayloadHash = "payload-hash"
		};
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			unit.LocalRegistrationRepo.Add(registration);
			unit.LocalSyncBatchRepo.Add(batch);
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision = long.MaxValue;
			unit.SystemConfigRepo.Update(config);
			await unit.SaveChanges();
		}
		using IServiceScope verify = _provider.CreateScope();
		IAppUnitOfWork read = verify.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		LocalSyncBatch loaded = (await read.LocalSyncBatchRepo.GetById(batch.Id))!;
		Assert.That((loaded.LocalRegistrationId, loaded.SyncKey, loaded.Revision, loaded.PayloadHash),
			Is.EqualTo((registration.Id, "batch-a", 9L, "payload-hash")));
		Assert.That(loaded.LocalRegistration.Id, Is.EqualTo(registration.Id));
		Assert.That(loaded.LocalRegistration.SyncDateTime, Is.EqualTo(registration.SyncDateTime));
		Assert.That(loaded.LocalRegistration.SyncDateTime!.Value.Kind, Is.EqualTo(DateTimeKind.Utc));
		Assert.That(loaded.LocalRegistration.SyncRevision, Is.EqualTo(8));
		Assert.That((await read.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(long.MaxValue));
	}

	[TestCase("registration")]
	[TestCase("batch")]
	[TestCase("foreignKey")]
	public async Task SynchronizationMetadata_RejectsDuplicateKeysAndMissingRegistration(string constraint)
	{
		Guid registrationId = Guid.NewGuid();
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = registrationId, LocalDatasetKey = "device-a" });
			unit.LocalSyncBatchRepo.Add(new LocalSyncBatch { Id = Guid.NewGuid(), LocalRegistrationId = registrationId, SyncKey = "batch-a", PayloadHash = "hash" });
			await unit.SaveChanges();
		}
		using IServiceScope duplicate = _provider.CreateScope();
		IAppUnitOfWork invalid = duplicate.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		if (constraint == "registration")
		{
			invalid.LocalRegistrationRepo.Add(new LocalRegistration { Id = Guid.NewGuid(), LocalDatasetKey = "device-a" });
		}
		else
		{
			invalid.LocalSyncBatchRepo.Add(new LocalSyncBatch
			{
				Id = Guid.NewGuid(), LocalRegistrationId = constraint == "foreignKey" ? Guid.NewGuid() : registrationId,
				SyncKey = "batch-a", PayloadHash = "different-hash"
			});
		}
		Assert.ThrowsAsync<DbUpdateException>(async () => await invalid.SaveChanges());
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task BatchTransaction_CommitsOrRollsBackBusinessRevisionAndReceiptTogether(bool commit)
	{
		Guid registrationId = Guid.NewGuid();
		using (IServiceScope setup = _provider.CreateScope())
		{
			IAppUnitOfWork unit = setup.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			unit.LocalRegistrationRepo.Add(new LocalRegistration { Id = registrationId, LocalDatasetKey = "device-a" });
			await unit.SaveChanges();
		}
		string originalName;
		using (IServiceScope scope = _provider.CreateScope())
		{
			IAppUnitOfWork unit = scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			await using DataAccess.Core.Entities.IUnitOfWorkTransaction transaction = await unit.BeginTransaction();
			Currency currency = (await unit.CurrencyRepo.GetById(EntitiesIds.BaseCurrencyId))!;
			originalName = currency.Name;
			currency.Name = "Batch changed name";
			unit.CurrencyRepo.Update(currency);
			SystemConfig config = (await unit.SystemConfigRepo.GetAll()).Single();
			config.CurrentRevision = 1;
			unit.SystemConfigRepo.Update(config);
			unit.LocalSyncBatchRepo.Add(new LocalSyncBatch
			{
				Id = Guid.NewGuid(), LocalRegistrationId = registrationId, SyncKey = "batch-a", Revision = 1, PayloadHash = "hash"
			});
			await unit.SaveChanges();
			if (commit) { await unit.CommitTransaction(transaction); }
			else { await unit.RollbackTransaction(transaction); }
		}
		using IServiceScope verify = _provider.CreateScope();
		IAppUnitOfWork read = verify.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		Assert.That((await read.CurrencyRepo.GetById(EntitiesIds.BaseCurrencyId))!.Name, Is.EqualTo(commit ? "Batch changed name" : originalName));
		Assert.That((await read.SystemConfigRepo.GetAll()).Single().CurrentRevision, Is.EqualTo(commit ? 1L : 0L));
		Assert.That(await read.LocalSyncBatchRepo.GetAll(), Has.Count.EqualTo(commit ? 1 : 0));
	}
}
