using System.Net.Http.Json;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Transactions;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using DataAccess.Contracts;
using DataAccess.EntityFramework;
using Dehb.WebApi;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace IntegrationTests;

/// <summary>
/// Exercises HTTP transaction endpoints through the real Business and SQLite stack.
/// Loads production host registrations without substituting services or repositories.
/// Seeds an isolated database with the required accounting configuration.
/// Keeps the database alive across independent HTTP request scopes.
/// Verifies JSON collection binding and persisted cumulative values.
/// Covers capped-selection refresh, range deletion and account-tree projection.
/// Uses the Remote category for the infrastructure integration suite.
/// Disposes the host, client and database after the test.
/// </summary>
[TestFixture(Category = "Remote")]
public sealed class TransactionEndpointTests
{
	[Test]
	public async Task TransactionFlow_PersistsRefreshesAndDeletesThroughHttp()
	{
		string connectionString = "Data Source=http-" + Guid.NewGuid() + ";Mode=Memory;Cache=Shared;Pooling=False";
		await using SqliteConnection databaseLifetime = new(connectionString);
		await databaseLifetime.OpenAsync();
		WebApplicationBuilder builder = WebApplication.CreateBuilder();
		builder.WebHost.UseTestServer();
		builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
		{
			["ConnectionStrings:AppDb"] = connectionString
		});
		builder.Services.AddWebApiDiConfiguration(builder.Configuration);
		await using WebApplication app = builder.Build();
		app.AddEndpointsConfiguration();
		Guid accountId = Guid.NewGuid();
		Guid otherId = Guid.NewGuid();
		using (IServiceScope seedScope = app.Services.CreateScope())
		{
			AppDbContext context = seedScope.ServiceProvider.GetRequiredService<AppDbContext>();
			await context.Database.EnsureCreatedAsync();
			IAppUnitOfWork unit = seedScope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
			Currency currency = new() { Id = Guid.NewGuid(), Name = "Euro", Code = "EUR", Symbol = "€" };
			AccountGroup group = new() { Id = Guid.NewGuid(), Name = "Accounts" };
			group.ParentId = group.Id;
			group.Parent = group;
			unit.CurrencyRepo.Add(currency);
			unit.AccountGroupRepo.Add(group);
			unit.AccountRepo.Add(new Account { Id = accountId, Name = "Main", GroupId = group.Id, Group = group, CurrencyId = currency.Id, Currency = currency });
			unit.AccountRepo.Add(new Account { Id = otherId, Name = "Other", Order = 1, GroupId = group.Id, Group = group, CurrencyId = currency.Id, Currency = currency });
			unit.SystemConfigRepo.Add(new SystemConfig { Id = Guid.NewGuid(), BaseCurrencyId = currency.Id, AmountPrecision = 2 });
			await unit.SaveChanges();
		}
		await app.StartAsync();
		using HttpClient client = app.GetTestClient();
		using HttpResponseMessage added = await client.PostAsJsonAsync("/transactions/add", new
		{
			dateTime = new DateTime(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc),
			entries = new[]
			{
				new { accountId, amount = 12.34m, rate = 1m },
				new { accountId = otherId, amount = -12.34m, rate = 1m }
			}
		});
		added.EnsureSuccessStatusCode();
		Guid id = await added.Content.ReadFromJsonAsync<Guid>();
		Assert.That(id, Is.Not.EqualTo(Guid.Empty));
		System.Text.Json.JsonSerializerOptions json = new(System.Text.Json.JsonSerializerDefaults.Web)
		{
			PreferredObjectCreationHandling = System.Text.Json.Serialization.JsonObjectCreationHandling.Populate
		};
		TransactionListInfo? list = await client.GetFromJsonAsync<TransactionListInfo>(
			$"/transactions/get-transactions-by-account?accountId={accountId}&date=2025-01-02", json);
		Assert.That(list!.Transactions, Has.Count.EqualTo(1));
		Assert.That(list.Transactions[0].Entries, Has.Count.EqualTo(2));
		Assert.That(list.Transactions[0].Entries[0].CumulativeAmount, Is.EqualTo(12.34m));

		Guid[] ids = [id, .. Enumerable.Range(0, 299).Select(_ => Guid.NewGuid())];
		using HttpResponseMessage refresh = await client.PostAsJsonAsync("/transactions/refresh-transactions", new
		{
			date = "2025-01-02", transactionIds = ids, accountId
		});
		refresh.EnsureSuccessStatusCode();
		TransactionRefreshInfo? refreshed = await refresh.Content.ReadFromJsonAsync<TransactionRefreshInfo>(json);
		Assert.That(refreshed!.Transactions.Single().Id, Is.EqualTo(id));
		Assert.That(refreshed.RemovedTransactionIds, Is.EquivalentTo(ids.Skip(1)));

		AccountTreeInfo? tree = await client.GetFromJsonAsync<AccountTreeInfo>("/account-groups/get-accounts-tree", json);
		Assert.That(tree!.Elements, Has.Count.EqualTo(2));
		Assert.That(tree.Elements.All(element => element.CurrencyName == "Euro"), Is.True);

		using HttpResponseMessage deleted = await client.PostAsJsonAsync("/transactions/delete-transactions-by-account", new
		{
			accountId, fromDate = "2025-01-01", toDate = "2025-01-02"
		});
		deleted.EnsureSuccessStatusCode();
		TransactionListInfo? remaining = await client.GetFromJsonAsync<TransactionListInfo>("/transactions/get-transactions?date=2025-01-02", json);
		Assert.That(remaining!.Transactions, Is.Empty);
	}
}