using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Exchange.Models.Enums;
using Master.Impl;
using Master.Impl.Operations.ApplySyncBatch;
using Master.Impl.Operations.RestoreConsistency;
using Master.Impl.Operations.ValidateSyncBatch;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts;
using System.Text.Json;
using Exchange.Models.Synchronizations;
using Business.Models.Exceptions;
using Master.Impl.Operations.MapSyncEntity;
using Master.Impl.Operations.MergeSyncBatch;

namespace UnitTests.Master;

/// <summary>
/// Exercises restoration and validation directly through their DI contracts.
/// Supplies detached data without repositories, files or a database.
/// Reuses each scoped operation across independent graphs with identical identities.
/// Verifies restoration traversal does not retain visited rows across calls.
/// Changes conflict preference and precision between validation invocations.
/// Confirms cancellation does not poison later calls on the same instance.
/// Model mutations are inspected through the supplied POCO graph.
/// Persistence and revision allocation belong to separate integration tests.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class SyncStateOperationTests
{
	private static readonly Guid CurrencyId = Guid.NewGuid();
	private static readonly Guid RateId = Guid.NewGuid();
	private static readonly Guid FirstCategoryId = Guid.NewGuid();
	private static readonly Guid SecondCategoryId = Guid.NewGuid();
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;

	[SetUp]
	public void SetUp()
	{
		ServiceCollection services = new();
		services.AddMasterModule();
		ISharedContext shared = Substitute.For<ISharedContext>();
		shared.LogService.Returns(Substitute.For<ILogService>());
		services.AddSingleton(shared);
		_provider = services.BuildServiceProvider();
		_scope = _provider.CreateScope();
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	[Test]
	public void RestoreReusesInstanceAfterCancellationAndAcrossGraphs()
	{
		IRestoreConsistencyOperation operation = _scope.ServiceProvider.GetRequiredService<IRestoreConsistencyOperation>();
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		Assert.Throws<OperationCanceledException>(() => operation.Run(Graph(), Config(), cancellation.Token));
		foreach (int attempt in new[] { 1, 2 })
		{
			Dictionary<string, SyncEntityState> graph = Graph();
			Currency currency = (Currency)graph[nameof(Currency)].Rows[CurrencyId].Entity;
			currency.DeleteRevision = attempt;
			graph[nameof(CurrencyRate)].Rows[RateId].Tracking.DeleteRevision = attempt;
			operation.Run(graph, Config());
			Assert.That(currency.DeleteRevision, Is.Null);
			Assert.That(graph[nameof(CurrencyRate)].Rows[RateId].Tracking.DeleteRevision, Is.Null);
		}
	}

	[Test]
	public void ValidationUsesEachCallsPriorityAndConfiguration()
	{
		IValidateSyncBatchOperation operation = _scope.ServiceProvider.GetRequiredService<IValidateSyncBatchOperation>();
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		Assert.Throws<OperationCanceledException>(() => operation.Run(Graph(), Config(), ConflictPriority.Local, cancellation.Token));
		foreach (ConflictPriority priority in new[] { ConflictPriority.Local, ConflictPriority.Master })
		{
			Dictionary<string, SyncEntityState> graph = Graph();
			SystemConfig config = Config();
			config.RatePrecision = priority == ConflictPriority.Local ? 2 : 4;
			CurrencyRate rate = (CurrencyRate)graph[nameof(CurrencyRate)].Rows[RateId].Entity;
			rate.Rate = 1.23456m;
			Guid foreignCurrencyId = Guid.NewGuid();
			graph[nameof(Currency)].Rows.Add(foreignCurrencyId, new SyncMergeRow
			{
				Entity = new Currency { Id = foreignCurrencyId, Code = "EUR", Name = "EUR", Symbol = "E", EnglishName = "Euro" }
			});
			rate.CurrencyId = foreignCurrencyId;
			rate.Currency = (Currency)graph[nameof(Currency)].Rows[foreignCurrencyId].Entity;
			Guid baseRateId = Guid.NewGuid();
			graph[nameof(CurrencyRate)].Rows.Add(baseRateId, new SyncMergeRow
			{
				Entity = new CurrencyRate
				{
					Id = baseRateId, CurrencyId = CurrencyId, Currency = (Currency)graph[nameof(Currency)].Rows[CurrencyId].Entity,
					Date = AppValues.InitialDate, Rate = 1m
				}
			});
			operation.Run(graph, config, priority);
			Assert.That(rate.Rate, Is.EqualTo(priority == ConflictPriority.Local ? 1.23m : 1.2346m));
			Guid preferred = priority == ConflictPriority.Local ? FirstCategoryId : SecondCategoryId;
			Guid renamed = priority == ConflictPriority.Local ? SecondCategoryId : FirstCategoryId;
			Assert.That(((Category)graph[nameof(Category)].Rows[preferred].Entity).Name, Is.EqualTo("Same"));
			Assert.That(((Category)graph[nameof(Category)].Rows[renamed].Entity).Name, Is.EqualTo("Same_1"));
		}
	}

	[Test]
	public void MergeReusesInstanceAfterCancellationAndDuplicateFailure()
	{
		IMergeSyncBatchOperation operation = _scope.ServiceProvider.GetRequiredService<IMergeSyncBatchOperation>();
		Guid id = Guid.NewGuid();
		SyncObject item = new() { Type = nameof(Category), Id = id, EditRevision = null, ModificationType = ModificationType.None };
		item.Properties.Add(nameof(Category.Name), JsonSerializer.SerializeToElement("New"));
		item.Properties.Add(nameof(Category.Order), JsonSerializer.SerializeToElement(0));
		SyncBatch batch = new() { SyncKey = "test", CreatedAt = DateTime.UtcNow, Priority = ConflictPriority.Local };
		batch.Added.Add(item);
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		Assert.Throws<OperationCanceledException>(() => operation.Run(batch, Guid.NewGuid(), 1, MergeGraph(), new(), cancellation.Token));
		batch.Added.Add(item);
		Assert.Throws<InvalidSyncBatchException>(() => operation.Run(batch, Guid.NewGuid(), 1, MergeGraph(), new()));
		batch.Added.RemoveAt(1);
		foreach (int revision in new[] { 1, 2 })
		{
			Dictionary<string, SyncEntityState> graph = MergeGraph();
			operation.Run(batch, Guid.NewGuid(), revision, graph, new());
			Assert.That(((Category)graph[nameof(Category)].Rows[id].Entity).Name, Is.EqualTo("New"));
		}
	}

	[Test]
	public void MappingReusesInstanceAcrossAggregateSchemas()
	{
		IMapSyncEntityOperation operation = _scope.ServiceProvider.GetRequiredService<IMapSyncEntityOperation>();
		SyncEntityState templateSet = new() { Type = nameof(Template), EntityType = typeof(Template), ContentFields = [nameof(Template.Entries)] };
		SyncObject template = new() { Type = nameof(Template), Id = Guid.NewGuid(), EditRevision = null, ModificationType = ModificationType.None };
		template.Properties.Add(nameof(Template.Entries), JsonSerializer.SerializeToElement(new[]
		{
			new { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), Position = 0, Amount = 7m }
		}));
		Template mappedTemplate = (Template)operation.Read(templateSet, template, true, false);
		Assert.That(mappedTemplate.Entries.Single().Amount, Is.EqualTo(7m));
		SyncMergeRow row = new() { Entity = mappedTemplate, Original = operation.Snapshot(templateSet, mappedTemplate) };
		Assert.That(operation.ContentChanged(templateSet, row), Is.False);
		mappedTemplate.Entries.Single().Amount = 8m;
		Assert.That(operation.ContentChanged(templateSet, row), Is.True);
		SyncEntityState transactionSet = new() { Type = nameof(Transaction), EntityType = typeof(Transaction), ContentFields = [nameof(Transaction.Entries)] };
		SyncObject transaction = new() { Type = nameof(Transaction), Id = Guid.NewGuid(), EditRevision = null, ModificationType = ModificationType.None };
		transaction.Properties.Add(nameof(Transaction.Entries), JsonSerializer.SerializeToElement(new[]
		{
			new { Id = Guid.NewGuid(), AccountId = Guid.NewGuid(), Position = 0, Amount = 9m, Rate = 2m }
		}));
		Transaction mappedTransaction = (Transaction)operation.Read(transactionSet, transaction, true, false);
		Assert.That(mappedTransaction.Entries.Single().Rate, Is.EqualTo(2m));
		Assert.Throws<InvalidSyncBatchException>(() => operation.Read(templateSet, transaction, true, false));
		Assert.That(((Template)operation.Read(templateSet, template, true, false)).Entries.Single().Amount, Is.EqualTo(7m));
	}

	[Test]
	public void MergeUsesInjectedMappingOperation()
	{
		IMapSyncEntityOperation mapping = Substitute.For<IMapSyncEntityOperation>();
		Dictionary<string, SyncEntityState> graph = MergeGraph();
		Guid id = Guid.NewGuid();
		SyncObject item = new() { Type = nameof(Category), Id = id, EditRevision = null, ModificationType = ModificationType.None };
		Category entity = new() { Id = id, Name = "Mapped by substitute" };
		mapping.Read(graph[nameof(Category)], item, true, true).Returns(entity);
		ServiceCollection services = new();
		services.AddMasterModule();
		services.AddSingleton(_scope.ServiceProvider.GetRequiredService<ISharedContext>());
		services.AddSingleton(mapping);
		using ServiceProvider provider = services.BuildServiceProvider();
		using IServiceScope scope = provider.CreateScope();
		SyncBatch batch = new() { SyncKey = "mapped", CreatedAt = DateTime.UtcNow, Priority = ConflictPriority.Local };
		batch.Added.Add(item);
		scope.ServiceProvider.GetRequiredService<IMergeSyncBatchOperation>().Run(batch, Guid.NewGuid(), 1, graph, new());
		mapping.Received(1).Read(graph[nameof(Category)], item, true, true);
		Assert.That(graph[nameof(Category)].Rows[id].Entity, Is.SameAs(entity));
	}

	private static Dictionary<string, SyncEntityState> MergeGraph()
	{
		SyncEntityState categories = new() { Type = nameof(Category), EntityType = typeof(Category), ContentFields = [nameof(Category.Name)], IsOrdered = true };
		SyncEntityState rates = new() { Type = nameof(CurrencyRate), EntityType = typeof(CurrencyRate) };
		return new[] { categories, rates }.ToDictionary(set => set.Type);
	}

	private static SystemConfig Config() => new() { BaseCurrencyId = CurrencyId, AmountPrecision = 2, RatePrecision = 4 };

	private static Dictionary<string, SyncEntityState> Graph()
	{
		SyncEntityState currencies = new() { Type = nameof(Currency), EntityType = typeof(Currency), IsOrdered = true };
		currencies.Rows.Add(CurrencyId, new SyncMergeRow { Entity = new Currency { Id = CurrencyId, Code = "USD", Name = "USD", Symbol = "$", EnglishName = "Dollar" } });
		SyncEntityState rates = new() { Type = nameof(CurrencyRate), EntityType = typeof(CurrencyRate) };
		rates.Rows.Add(RateId, new SyncMergeRow { Entity = new CurrencyRate { Id = RateId, CurrencyId = CurrencyId, Currency = (Currency)currencies.Rows[CurrencyId].Entity, Date = AppValues.InitialDate, Rate = 1m } });
		SyncEntityState accounts = new() { Type = nameof(Account), EntityType = typeof(Account) };
		accounts.Rows.Add(EntitiesIds.BalancingAccountId, new SyncMergeRow { Entity = new Account { Id = EntitiesIds.BalancingAccountId, Name = "Balance", CurrencyId = CurrencyId } });
		SyncEntityState groups = new() { Type = nameof(CategoryGroup), EntityType = typeof(CategoryGroup) };
		groups.Rows.Add(RootsIds.CategoryGroupId, new SyncMergeRow { Entity = new CategoryGroup { Id = RootsIds.CategoryGroupId, Name = "Root", ParentId = RootsIds.CategoryGroupId } });
		SyncEntityState categories = new() { Type = nameof(Category), EntityType = typeof(Category), ParentType = nameof(CategoryGroup), ParentField = nameof(Category.GroupId), IsOrdered = true };
		categories.Rows.Add(FirstCategoryId, new SyncMergeRow { Entity = new Category { Id = FirstCategoryId, Name = "Same", GroupId = RootsIds.CategoryGroupId }, IncomingContent = true });
		categories.Rows.Add(SecondCategoryId, new SyncMergeRow { Entity = new Category { Id = SecondCategoryId, Name = "Same", GroupId = RootsIds.CategoryGroupId } });
		return new[] { currencies, rates, accounts, groups, categories }.ToDictionary(set => set.Type);
	}
}
