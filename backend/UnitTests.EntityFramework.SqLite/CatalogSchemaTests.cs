using Business.Contracts.Services;
using Business.Contracts.Utils.Models;
using Business.Impl;
using Business.Models.Constants;
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
		Assert.That(await check.ExecuteScalarAsync(), Is.EqualTo(1L));
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
}
