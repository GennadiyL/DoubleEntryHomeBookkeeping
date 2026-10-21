using System.Text.Json;
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
	private static readonly string[] OrderPropertyNames = ["order"];
	private static readonly string[] CurrencyContentPropertyNames = ["code", "symbol", "englishName", "name", "isFavorite"];
	private ServiceProvider _provider = null!;
	private IServiceScope _scope = null!;
	private IAppUnitOfWork _unit = null!;
	private ISharedContext _shared = null!;
	private ICaptureChangesOperation _operation = null!;
	private readonly DateTime _now = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

	[SetUp]
	public void SetUp()
	{
		_unit = Substitute.For<IAppUnitOfWork>();
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
		Empty(_unit.SystemConfigRepo);
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddSynchronizationModule();
		services.AddScoped<IAppUnitOfWork>(_ => _unit);
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

	[Test]
	public async Task Run_ClassifiesChangesWithoutAcceptingThem()
	{
		Currency added = Currency();
		Currency edited = Currency(); edited.EditRevision = 7; edited.ModificationType = ModificationType.Content;
		Currency deleted = Currency(); deleted.EditRevision = 0; deleted.DeleteRevision = 0; deleted.ModificationType = ModificationType.Content;
		Currency discarded = Currency(); discarded.DeleteRevision = 0; discarded.ModificationType = ModificationType.Content;
		Currency captured = Currency(); captured.EditRevision = 0; captured.DeleteRevision = 0;
		_unit.CurrencyRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { added, edited, deleted, discarded, captured });
		SyncBatch batch = await _operation.Run();
		Assert.Multiple(() =>
		{
			Assert.That(batch.Added.Select(item => item.Id), Is.EqualTo(new[] { added.Id }));
			Assert.That(batch.Updated.Select(item => item.Id), Is.EqualTo(new[] { edited.Id }));
			Assert.That(batch.Deleted.Select(item => item.Id), Is.EqualTo(new[] { deleted.Id }));
			Assert.That(batch.CreatedAt, Is.EqualTo(_now));
			Assert.That(batch.Priority, Is.EqualTo(Exchange.Models.Enums.ConflictPriority.Local));
			Assert.That(Guid.TryParse(batch.SyncKey, out _), Is.True);
			Assert.That(added.EditRevision, Is.Null);
			Assert.That(edited.ModificationType, Is.EqualTo(ModificationType.Content));
			Assert.That(deleted.DeleteRevision, Is.Zero);
			Assert.That(batch.Deleted.Single().Properties["name"].GetString(), Is.EqualTo(deleted.Name));
		});
		await _unit.DidNotReceive().SaveChanges(Arg.Any<CancellationToken>());
		_unit.CurrencyRepo.DidNotReceive().Update(Arg.Any<Currency>());
	}

	[Test]
	public async Task Run_SeparatesOrderFromWholeContent()
	{
		Currency order = Currency(); order.EditRevision = 5; order.Order = 3; order.ModificationType = ModificationType.Order;
		Currency content = Currency(); content.EditRevision = 5; content.ModificationType = ModificationType.Content;
		Currency both = Currency(); both.EditRevision = 5; both.ModificationType = ModificationType.Content | ModificationType.Order;
		_unit.CurrencyRepo.GetChanges(Arg.Any<CancellationToken>()).Returns(new[] { order, content, both });
		SyncBatch batch = await _operation.Run();
		Assert.Multiple(() =>
		{
			Assert.That(batch.Updated[0].Properties.Keys, Is.EqualTo(OrderPropertyNames));
			Assert.That(batch.Updated[0].Properties["order"].GetInt32(), Is.EqualTo(3));
			Assert.That(batch.Updated[1].Properties.Keys, Is.EquivalentTo(CurrencyContentPropertyNames));
			Assert.That(batch.Updated[2].Properties.ContainsKey("order"), Is.True);
			Assert.That((int)batch.Updated[2].ModificationType, Is.EqualTo(3));
		});
		content.Name = "Later";
		Assert.That(batch.Updated[1].Properties["name"].GetString(), Is.EqualTo("Euro"));
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
		JsonElement entries = batch.Added.Single(item => item.Type == "transaction").Properties["entries"];
		Assert.Multiple(() =>
		{
			Assert.That(entries.GetArrayLength(), Is.EqualTo(2));
			Assert.That(entries[0].GetProperty("position").GetInt32(), Is.Zero);
			Assert.That(entries[1].GetProperty("amount").GetDecimal(), Is.EqualTo(2.1234m));
			Assert.That(entries[1].GetProperty("rate").GetDecimal(), Is.EqualTo(1.25m));
			Assert.That(entries[0].TryGetProperty("cumulativeAmount", out _), Is.False);
			Assert.That(entries[0].TryGetProperty("account", out _), Is.False);
			Assert.That(entries[0].TryGetProperty("transaction", out _), Is.False);
			Assert.That(batch.Added.Single(item => item.Type == "template").Properties["entries"].GetArrayLength(), Is.EqualTo(1));
		});
		SyncBatch copy = _shared.JsonService.Deserialize<SyncBatch>(_shared.JsonService.Serialize(batch))!;
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

	private static Currency Currency() => new() { Id = Guid.NewGuid(), Code = "EUR", Symbol = "E", EnglishName = "Euro", Name = "Euro" };

	private static void Empty<T>(ITrackedRepository<T> repository) where T : class, IBaseEntity, ITrackedEntity
	{
		repository.GetChanges(Arg.Any<CancellationToken>()).Returns(Array.Empty<T>());
	}
}