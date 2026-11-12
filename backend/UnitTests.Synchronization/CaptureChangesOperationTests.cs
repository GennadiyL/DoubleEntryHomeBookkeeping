using Business.Models.Entities.Base;
using System.Text.Json;
using Business.Contracts.Utils.Merging;
using Business.Core.Entities;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Exchange.Models.Synchronizations;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using NUnit.Framework;
using Shared.Contracts;
using Shared.Impl;
using Synchronization.Impl;
using Synchronization.Impl.Operations.CaptureChanges;

namespace UnitTests.Synchronization;

/// <summary>
/// Verifies detached capture through the registered internal operation.
/// Repositories are substituted so no user database or network is accessed.
/// Real JSON serialization checks the exchange property representation.
/// Cases cover creation, editing, deletion and independent ordering.
/// Owned entries are captured without navigation graphs or derived caches.
/// Tracking state remains unchanged until separate capture acceptance.
/// Local configuration supplies only the captured conflict priority.
/// Failed reads and cancellation never return a partial successful batch.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class CaptureChangesOperationTests
{
	private static readonly string[] OrderPropertyNames = [nameof(IOrderedEntity.Order)];
	private static readonly string[] CurrencyContentPropertyNames = [nameof(Currency.Code), nameof(Currency.Symbol), nameof(Currency.EnglishName), nameof(CatalogEntity.Name), nameof(CatalogEntity.IsFavorite)];
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private ILocalUnitOfWork _unit = null!;
	private ISharedContext _shared = null!;
	private ICaptureChangesOperation _operation = null!;
	private readonly DateTime _now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

	[SetUp]
	public void SetUp()
	{
		_unit = Substitute.For<ILocalUnitOfWork>();
		_shared = Substitute.For<ISharedContext>();
		_shared.DateTimeService.UtcNow.Returns(_now);
		_unit.LocalConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(new List<LocalConfig> { new() });
		Empty(_unit.AccountRepo);
		Empty(_unit.AccountGroupRepo);
		Empty(_unit.CategoryRepo);
		Empty(_unit.CategoryGroupRepo);
		Empty(_unit.CorrespondentRepo);
		Empty(_unit.CorrespondentGroupRepo);
		Empty(_unit.ProjectRepo);
		Empty(_unit.ProjectGroupRepo);
		Empty(_unit.ReportRepo);
		Empty(_unit.ReportGroupRepo);
		Empty(_unit.TemplateRepo);
		Empty(_unit.TemplateGroupRepo);
		Empty(_unit.TransactionRepo);
		Empty(_unit.CurrencyRepo);
		Empty(_unit.CurrencyRateRepo);
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddSynchronizationModule();
		IUnitOfWorkFactory factory = Substitute.For<IUnitOfWorkFactory>();
		factory.CreateLocal().Returns(_unit);
		services.AddScoped<IUnitOfWorkFactory>(_ => factory);
		services.AddScoped<ISharedContext>(_ => _shared);
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		_shared.JsonService.Returns(_scope.ServiceProvider.GetRequiredService<IJsonService>());
		_operation = _scope.ServiceProvider.GetRequiredService<ICaptureChangesOperation>();
	}

	[TearDown]
	public void TearDown()
	{
		_scope.Dispose();
		_provider.Dispose();
	}

	[TestCase(null, ModificationType.None)]
	[TestCase(7L, ModificationType.None)]
	[TestCase(7L, ModificationType.Content)]
	[TestCase(7L, ModificationType.Order)]
	[TestCase(7L, ModificationType.Content | ModificationType.Order)]
	public async Task DeleteRestoreBeforeCapturePreservesOriginalState(long? revision, ModificationType flags)
	{
		Currency entity = CreateCurrency();
		entity.EditRevision = revision;
		entity.ModificationType = flags;
		_unit.CurrencyRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { entity });
		entity.SetDeleted();
		Assert.That(entity.ModificationType, Is.EqualTo(flags));
		SyncBatch deleted = await _operation.Run();
		Assert.That(deleted.Deleted.Single().EditRevision, Is.EqualTo(revision));
		Assert.That(deleted.Deleted.Single().ModificationType.HasFlag(Exchange.Models.Enums.ModificationType.Content), Is.True);
		entity.SetAlive();
		Assert.That(entity.EditRevision, Is.EqualTo(revision));
		Assert.That(entity.DeleteRevision, Is.Null);
		Assert.That(entity.ModificationType, Is.EqualTo(flags));
		SyncBatch restored = await _operation.Run();
		Assert.That(restored.Deleted, Is.Empty);
		Assert.That(restored.Added.Count, Is.EqualTo(revision is null ? 1 : 0));
		Assert.That(restored.Updated.Count, Is.EqualTo(revision is not null && flags != ModificationType.None ? 1 : 0));
	}

	[Test]
	public async Task Run_ClassifiesChangesWithoutAcceptingThem()
	{
		Currency added = CreateCurrency();
		Currency edited = CreateCurrency(); edited.EditRevision = 7; edited.ModificationType = ModificationType.Content;
		Currency deleted = CreateCurrency(); deleted.EditRevision = 0; deleted.DeleteRevision = 0; deleted.ModificationType = ModificationType.Content;
		Currency discarded = CreateCurrency(); discarded.DeleteRevision = 0;
		Currency captured = CreateCurrency(); captured.EditRevision = 0; captured.DeleteRevision = 0;
		_unit.CurrencyRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { added, edited, deleted, discarded, captured });
		SyncBatch batch = await _operation.Run();
		Assert.Multiple(() =>
		{
			Assert.That(batch.Added.Select(item => item.Id), Is.EqualTo(new[] { added.Id }));
			Assert.That(batch.Updated.Select(item => item.Id), Is.EqualTo(new[] { edited.Id }));
			Assert.That(batch.Deleted.Select(item => item.Id), Is.EqualTo(new[] { deleted.Id, discarded.Id, captured.Id }));
			Assert.That(batch.CreatedAt, Is.EqualTo(_now));
			Assert.That(batch.Priority, Is.EqualTo(Exchange.Models.Enums.ConflictPriority.Local));
			Assert.That(Guid.TryParse(batch.SyncKey, out _), Is.True);
			Assert.That(added.EditRevision, Is.Null);
			Assert.That(edited.ModificationType, Is.EqualTo(ModificationType.Content));
			Assert.That(deleted.DeleteRevision, Is.Zero);
			Assert.That(batch.Deleted.First().Properties[nameof(CatalogEntity.Name)].GetString(), Is.EqualTo(deleted.Name));
		});
		await _unit.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
		_unit.CurrencyRepo.DidNotReceive().Update(Arg.Any<Currency>());
	}

	[Test]
	public async Task Run_SeparatesOrderFromWholeContent()
	{
		Currency order = CreateCurrency(); order.EditRevision = 5; order.Order = 3; order.ModificationType = ModificationType.Order;
		Currency content = CreateCurrency(); content.EditRevision = 5; content.ModificationType = ModificationType.Content;
		Currency both = CreateCurrency(); both.EditRevision = 5; both.ModificationType = ModificationType.Content | ModificationType.Order;
		_unit.CurrencyRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { order, content, both });
		SyncBatch batch = await _operation.Run();
		Assert.Multiple(() =>
		{
			Assert.That(batch.Updated[0].Properties.Keys, Is.EqualTo(OrderPropertyNames));
			Assert.That(batch.Updated[0].Properties[nameof(IOrderedEntity.Order)].GetInt32(), Is.EqualTo(3));
			Assert.That(batch.Updated[1].Properties.Keys, Is.EquivalentTo(CurrencyContentPropertyNames));
			Assert.That(batch.Updated[2].Properties.ContainsKey(nameof(IOrderedEntity.Order)), Is.True);
			Assert.That((int)batch.Updated[2].ModificationType, Is.EqualTo(3));
		});
		content.Name = "Later";
		Assert.That(batch.Updated[1].Properties[nameof(CatalogEntity.Name)].GetString(), Is.EqualTo("Euro"));
	}

	[Test]
	public async Task Run_CapturesOrderedEntriesWithoutCachesOrNavigations()
	{
		Transaction transaction = new() { Id = Guid.NewGuid(), DateTime = _now, State = TransactionState.Draft };
		Guid accountId = Guid.NewGuid();
		transaction.Entries.Add(new TransactionEntry { Id = Guid.NewGuid(), Transaction = transaction, Account = null!, AccountId = accountId, Position = 1, Amount = 2.1234m, Rate = 1.25m, CumulativeAmount = 999m });
		transaction.Entries.Add(new TransactionEntry { Id = Guid.NewGuid(), Transaction = transaction, Account = null!, AccountId = accountId, Position = 0, Amount = -2.1234m, Rate = 1.25m });
		Template template = new() { Id = Guid.NewGuid(), GroupId = Guid.NewGuid(), Name = "Template" };
		template.Entries.Add(new TemplateEntry { Id = Guid.NewGuid(), Template = template, Account = null!, AccountId = accountId, Position = 0, Amount = 4m });
		_unit.TransactionRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { transaction });
		_unit.TemplateRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { template });
		SyncBatch batch = await _operation.Run();
		JsonElement entries = batch.Added.Single(item => item.Type == nameof(Transaction)).Properties[nameof(Transaction.Entries)];
		Assert.Multiple(() =>
		{
			Assert.That(entries.GetArrayLength(), Is.EqualTo(2));
			Assert.That(entries[0].GetProperty(nameof(TransactionEntry.Position)).GetInt32(), Is.Zero);
			Assert.That(entries[1].GetProperty(nameof(TransactionEntry.Amount)).GetDecimal(), Is.EqualTo(2.1234m));
			Assert.That(entries[1].GetProperty(nameof(TransactionEntry.Rate)).GetDecimal(), Is.EqualTo(1.25m));
			Assert.That(entries[0].TryGetProperty(nameof(TransactionEntry.CumulativeAmount), out _), Is.False);
			Assert.That(entries[0].TryGetProperty(nameof(Account), out _), Is.False);
			Assert.That(entries[0].TryGetProperty(nameof(Transaction), out _), Is.False);
			Assert.That(batch.Added.Single(item => item.Type == nameof(Template)).Properties[nameof(Transaction.Entries)].GetArrayLength(), Is.EqualTo(1));
		});
		string json = _shared.JsonService.Serialize(batch);
		using JsonDocument document = JsonDocument.Parse(json);
		JsonElement serializedTransaction = document.RootElement.GetProperty("Added").EnumerateArray()
			.Single(item => item.GetProperty("Type").GetString() == "Transaction");
		Assert.That(serializedTransaction.GetProperty("Properties").GetProperty("Entries")[0]
			.GetProperty("AccountId").GetGuid(), Is.EqualTo(accountId));
		SyncBatch copy = _shared.JsonService.Deserialize<SyncBatch>(json)!;
		Assert.That(copy.Added, Has.Count.EqualTo(2));
	}

	[Test]
	public async Task Run_EmptyCaptureHasNoLocalConfigurationPayload()
	{
		SyncBatch batch = await _operation.Run();
		Assert.That(batch.Added.Concat(batch.Updated).Concat(batch.Deleted), Is.Empty);
		Assert.That((await _operation.Run()).SyncKey, Is.Not.EqualTo(batch.SyncKey));
	}

	[Test]
	public void Run_RejectsMissingConfiguration()
	{
		_unit.LocalConfigRepo.GetAll(Arg.Any<CancellationToken>()).Returns(Array.Empty<LocalConfig>());
		Assert.ThrowsAsync<InvalidOperationException>(() => _operation.Run());
	}

	[Test]
	public void Run_PropagatesCancellationAndReadFailure()
	{
		using CancellationTokenSource cancellation = new();
		cancellation.Cancel();
		Assert.ThrowsAsync<OperationCanceledException>(() => _operation.Run(cancellation.Token));
		_unit.TransactionRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(Task.FromException<ICollection<Transaction>>(new InvalidOperationException("read failed")));
		Assert.ThrowsAsync<InvalidOperationException>(() => _operation.Run());
	}

	private static Currency CreateCurrency() => new() { Id = Guid.NewGuid(), Code = "EUR", Symbol = "E", EnglishName = "Euro", Name = "Euro" };

	private static void Empty<T>(ITrackedRepository<T> repository) where T : class, IBaseEntity, ITrackedEntity
	{
		repository.GetChanges(Arg.Any<CancellationToken>()).Returns(Array.Empty<T>());
	}
}
