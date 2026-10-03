using Business.Contracts.Services;
using Business.Contracts.Services.Transactions;
using Business.Impl;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using DataAccess.EntityFramework;
using DataAccess.EntityFramework.SqLite;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Shared.Impl;
using Tests.Common.DiConfigurations;

namespace UnitTests.EntityFramework.SqLite;

/// <summary>
/// Exercises SQLite cumulative SQL and its real service transaction boundaries.
/// Each test uses a fresh in-memory database and production DAL registration.
/// Fixtures seed the required currency, account group and system configuration.
/// Tests cover binary ordering, ranged rebuilding and absent predecessors.
/// Mutation tests verify downstream balances and aggregate replacement.
/// Overflow must restore both business rows and derived cache values.
/// Capped reads preserve complete aggregates and refresh without refill.
/// Shared non-database dependencies use the standard local test module.
/// </summary>
[System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1001", Justification = "NUnit TearDown disposes the database lifetime connection after each test.")]
[TestFixture(Category = "Local")]
public sealed class CumulativeTransactionTests
{
	private ServiceProvider _provider = null!;
	private SqliteConnection _databaseLifetime = null!;
	private IServiceScope _scope = null!;
	private AppDbContext _context = null!;
	private IAppUnitOfWork _unitOfWork = null!;
	private ITransactionService _service = null!;
	private ICumulativeService _cumulative = null!;
	private Account _account = null!;
	private Account _other = null!;
	private Account _destination = null!;
	private readonly DateTime _date = new(2025, 1, 1, 12, 0, 0, DateTimeKind.Utc);

	[SetUp]
	public async Task SetUp()
	{
		ServiceCollection services = new();
		services.AddSharedModule();
		services.AddSharedMockModule();
		services.AddBusinessModule();
		string connectionString = "Data Source=rollback-" + Guid.NewGuid() + ";Mode=Memory;Cache=Shared;Pooling=False";
		_databaseLifetime = new SqliteConnection(connectionString);
		await _databaseLifetime.OpenAsync();
		IConfiguration configuration = new ConfigurationBuilder().AddInMemoryCollection(
			new Dictionary<string, string?> { ["ConnectionStrings:AppDb"] = connectionString }).Build();
		services.AddDataAccessSqLiteModule(configuration);
		_provider = services.BuildServiceProvider(true);
		_scope = _provider.CreateScope();
		_context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
		await _context.Database.OpenConnectionAsync();
		await _context.Database.EnsureCreatedAsync();
		_unitOfWork = _scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		_service = _scope.ServiceProvider.GetRequiredService<ITransactionService>();
		_cumulative = _scope.ServiceProvider.GetRequiredService<ICumulativeService>();
		Currency currency = new() { Id = Guid.NewGuid(), Code = "EUR", Name = "Euro", Symbol = "€" };
		AccountGroup group = new() { Id = Guid.NewGuid(), Name = "Accounts" };
		group.ParentId = group.Id;
		group.Parent = group;
		_account = new Account { Id = Guid.NewGuid(), Name = "Main", CurrencyId = currency.Id, Currency = currency, GroupId = group.Id, Group = group };
		_other = new Account { Id = Guid.NewGuid(), Name = "Other", CurrencyId = currency.Id, Currency = currency, GroupId = group.Id, Group = group, Order = 1 };
		_destination = new Account { Id = Guid.NewGuid(), Name = "Destination", CurrencyId = currency.Id, Currency = currency, GroupId = group.Id, Group = group, Order = 2 };
		_unitOfWork.CurrencyRepo.Add(currency);
		_unitOfWork.AccountGroupRepo.Add(group);
		_unitOfWork.AccountRepo.Add(_account);
		_unitOfWork.AccountRepo.Add(_other);
		_unitOfWork.AccountRepo.Add(_destination);
		_unitOfWork.SystemConfigRepo.Add(new SystemConfig { Id = Guid.NewGuid(), BaseCurrencyId = currency.Id, AmountPrecision = 4 });
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
	}

	[TearDown]
	public void TearDown()
	{
		_scope?.Dispose();
		_provider?.Dispose();
		_databaseLifetime?.Dispose();
	}

	[Test]
	public async Task Rebuild_UsesBinaryGuidAndPositionOrder_ExcludesDraftAndDeleted()
	{
		Guid first = Guid.Parse("00000100-0000-0000-0000-000000000000");
		Guid second = Guid.Parse("00000001-0000-0000-0000-000000000000");
		Seed(first, _date, TransactionState.Confirmed, null, (_account, 10m), (_account, 5m), (_other, -15m));
		Seed(second, _date, TransactionState.Confirmed, null, (_account, -3m), (_other, 3m));
		Guid draft = Guid.NewGuid();
		Guid deleted = Guid.NewGuid();
		Seed(draft, _date.AddHours(-1), TransactionState.Draft, null, (_account, 100m));
		Seed(deleted, _date.AddHours(-2), TransactionState.Confirmed, 0, (_account, 200m), (_other, -200m));
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();

		await _cumulative.RebuildAsync();

		TransactionInfo firstInfo = await _service.GetById(first);
		TransactionInfo secondInfo = await _service.GetById(second);
		TransactionInfo draftInfo = await _service.GetById(draft);
		Assert.Multiple(() =>
		{
			Assert.That(firstInfo.Entries.Select(entry => entry.CumulativeAmount), Is.EqualTo(new[] { 10m, 15m, -15m }));
			Assert.That(secondInfo.Entries[0].CumulativeAmount, Is.EqualTo(12m));
			Assert.That(draftInfo.Entries[0].CumulativeAmount, Is.Zero);
		});
		Assert.That((await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(deleted)).First().CumulativeAmount, Is.EqualTo(77m));
		Assert.That((await _unitOfWork.TransactionRepo.GetByIdAsync(first))!.ModificationType, Is.EqualTo(ModificationType.None));
		Assert.That(await _unitOfWork.TransactionEntryRepo.GetPreviousAsync(_account.Id, _date.AddTicks(1)), Is.EqualTo(12m));
		Assert.That(await _unitOfWork.TransactionEntryRepo.GetPreviousAsync(_account.Id, _date), Is.Null);
	}

	[Test]
	public async Task Range_Rebuild_UsesSeedAndLeavesEarlierCacheUntouched()
	{
		Guid earlier = Guid.NewGuid();
		Guid later = Guid.NewGuid();
		Seed(earlier, _date, TransactionState.Confirmed, null, (_account, 50m), (_other, -50m));
		Seed(later, _date.AddDays(1), TransactionState.Confirmed, null, (_account, -10m), (_other, 10m));
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
		DataAccess.Core.Entities.IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransactionAsync();
		await _scope.ServiceProvider.GetRequiredService<ICumulativeAmountCommand>().RecalculateAsync(_account.Id, _date.AddDays(1), 50m);
		await _unitOfWork.CommitTransactionAsync(transaction);
		Assert.That((await _service.GetById(earlier)).Entries[0].CumulativeAmount, Is.EqualTo(77m));
		Assert.That((await _service.GetById(later)).Entries[0].CumulativeAmount, Is.EqualTo(40m));
		Assert.That((await _service.GetById(later)).Entries[1].CumulativeAmount, Is.EqualTo(77m));
	}

	[Test]
	public async Task Save_StateTransitionsAndDeletion_RecalculateFollowingEntries()
	{
		Guid first = await _service.Add(Input(_date, 50m));
		Guid second = await _service.Add(Input(_date.AddDays(1), 20m));
		Assert.That((await _service.GetById(second)).Entries[0].CumulativeAmount, Is.EqualTo(70m));

		TransactionParam draft = new() { DateTime = _date };
		draft.Entries.Add(new TransactionEntryParam { AccountId = _account.Id, Amount = 100m, Rate = 1m });
		await _service.Update(first, draft);
		Assert.That((await _service.GetById(first)).State, Is.EqualTo(TransactionState.Draft));
		Assert.That((await _service.GetById(first)).Entries[0].CumulativeAmount, Is.Zero);
		Assert.That((await _service.GetById(second)).Entries[0].CumulativeAmount, Is.EqualTo(20m));

		await _service.Update(first, Input(_date, 30m));
		Assert.That((await _service.GetById(second)).Entries[0].CumulativeAmount, Is.EqualTo(50m));
		await _service.Delete(first);
		Assert.That((await _service.GetById(second)).Entries[0].CumulativeAmount, Is.EqualTo(20m));
	}

	[Test]
	public async Task Update_AccountAndDate_RecalculatesOldAndNewHistories()
	{
		Guid moving = await _service.Add(Input(_date, 50m));
		Guid following = await _service.Add(Input(_date.AddDays(1), 20m));
		TransactionParam changed = Input(_date.AddDays(2), 40m);
		changed.Entries[0].AccountId = _destination.Id;
		await _service.Update(moving, changed);
		Assert.That((await _service.GetById(following)).Entries[0].CumulativeAmount, Is.EqualTo(20m));
		TransactionInfo moved = await _service.GetById(moving);
		Assert.That(moved.Entries[0].CumulativeAmount, Is.EqualTo(40m));
		Assert.That(moved.Entries[1].CumulativeAmount, Is.EqualTo(-60m));
	}

	[Test]
	public async Task Overflow_RollsBackNewAggregateAndRequiresNewContext()
	{
		Guid first = await _service.Add(Input(_date, AppValues.MaxDecimal));
		Assert.ThrowsAsync<SqliteException>(async () => await _service.Add(Input(_date.AddDays(1), 0.0001m)));
		StartNewScopeAfterRollback();
		TransactionListInfo list = await _service.GetTransactions(new DateOnly(2025, 1, 5));
		Assert.That(list.Transactions, Has.Count.EqualTo(1));
		Assert.That((await _service.GetById(first)).Entries[0].CumulativeAmount, Is.EqualTo(AppValues.MaxDecimal));
		Guid next = await _service.Add(Input(_date.AddDays(2), -1m));
		Assert.That((await _service.GetById(next)).Entries[0].CumulativeAmount, Is.EqualTo(AppValues.MaxDecimal - 1m));
	}

	[Test]
	public async Task Navigation_CapsWholeTransactions_AndRefreshDoesNotRefill()
	{
		for (int index = 0; index < 301; index++)
		{
			Seed(Guid.NewGuid(), _date.AddMinutes(index), TransactionState.Confirmed, null, (_account, 1m), (_other, -1m));
		}
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
		await _cumulative.RebuildAsync();
		DateOnly selected = new(2025, 1, 2);
		TransactionListInfo initial = await _service.GetTransactionsByAccount(_account.Id, selected);
		Assert.That(initial.Transactions, Has.Count.EqualTo(300));
		Assert.That(initial.LimitExceeded, Is.True);
		Assert.That(initial.Transactions.All(transaction => transaction.Entries.Count == 2), Is.True);
		Assert.That(initial.Transactions[0].Entries[0].CumulativeAmount, Is.EqualTo(301m));

		Guid edited = initial.Transactions[4].Id;
		await _service.Update(edited, Input(_date.AddDays(10), 1m));
		TransactionRefreshParam refresh = new() { Date = selected, AccountId = _account.Id };
		refresh.TransactionIds.AddRange(initial.Transactions.Select(transaction => transaction.Id));
		TransactionRefreshInfo result = await _service.RefreshTransactions(refresh);
		Assert.That(result.Transactions, Has.Count.EqualTo(299));
		Assert.That(result.RemovedTransactionIds, Is.EqualTo(new[] { edited }));
		Assert.That(result.Transactions[0].Entries[0].CumulativeAmount, Is.EqualTo(300m));
		TransactionListInfo navigated = await _service.GetTransactionsByAccount(_account.Id, selected);
		Assert.That(navigated.Transactions, Has.Count.EqualTo(300));
		Assert.That(navigated.LimitExceeded, Is.False);
	}

	[Test]
	public async Task BulkDelete_RecalculatesAllAccountsAndIsNotCapped()
	{
		Guid keep = await _service.Add(Input(_date.AddDays(2), 5m));
		for (int index = 0; index < 301; index++)
		{
			Seed(Guid.NewGuid(), _date.AddMinutes(index), TransactionState.Confirmed, null, (_account, 1m), (_other, -1m));
		}
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
		await _cumulative.RebuildAsync();
		await _service.DeleteTransactionsByAccount(_account.Id, new DateOnly(2024, 12, 31), new DateOnly(2025, 1, 2));
		Assert.That((await _service.GetById(keep)).Entries[0].CumulativeAmount, Is.EqualTo(5m));
		Assert.That((await _service.GetTransactions(new DateOnly(2025, 1, 5))).Transactions, Has.Count.EqualTo(1));
	}

	[Test]
	public async Task CombineAccounts_RebuildsDestinationInTheSameAction()
	{
		Guid source = await _service.Add(Input(_date, 50m));
		TransactionParam destination = Input(_date.AddDays(1), 20m);
		destination.Entries[0].AccountId = _destination.Id;
		Guid later = await _service.Add(destination);
		_context.ChangeTracker.Clear();
		await _scope.ServiceProvider.GetRequiredService<IAccountService>().CombineElements(_destination.Id, _account.Id);
		Assert.That((await _service.GetById(source)).Entries[0].AccountId, Is.EqualTo(_destination.Id));
		Assert.That((await _service.GetById(later)).Entries[0].CumulativeAmount, Is.EqualTo(70m));
	}

	[Test]
	public async Task UpdateOverflow_RestoresOriginalEntriesAndBalances()
	{
		await _service.Add(Input(_date, AppValues.MaxDecimal));
		Guid edited = await _service.Add(Input(_date.AddDays(1), -1m));
		ICollection<TransactionEntry> original = await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(edited);
		Assert.ThrowsAsync<SqliteException>(async () => await _service.Update(edited, Input(_date.AddDays(1), 1m)));
		StartNewScopeAfterRollback();
		TransactionInfo restored = await _service.GetById(edited);
		ICollection<TransactionEntry> restoredEntries = await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(edited);
		Assert.That(restoredEntries.Select(entry => entry.Id), Is.EquivalentTo(original.Select(entry => entry.Id)));
		Assert.That(restored.Entries[0].Amount, Is.EqualTo(-1m));
		Assert.That(restored.Entries[0].CumulativeAmount, Is.EqualTo(AppValues.MaxDecimal - 1m));
	}

	[Test]
	public async Task CombineOverflow_RestoresSourceAndEntryReferences()
	{
		Guid first = await _service.Add(Input(_date, AppValues.MaxDecimal));
		TransactionParam transfer = new() { DateTime = _date.AddDays(1) };
		transfer.Entries.Add(new TransactionEntryParam { AccountId = _destination.Id, Amount = 1m, Rate = 1m });
		transfer.Entries.Add(new TransactionEntryParam { AccountId = _account.Id, Amount = -1m, Rate = 1m });
		await _service.Add(transfer);
		_context.ChangeTracker.Clear();
		IAccountService accounts = _scope.ServiceProvider.GetRequiredService<IAccountService>();
		Assert.ThrowsAsync<SqliteException>(async () => await accounts.CombineElements(_destination.Id, _account.Id));
		StartNewScopeAfterRollback();
		Account? source = await _unitOfWork.AccountRepo.GetByIdAsync(_account.Id);
		Assert.That(source!.DeleteRevision, Is.Null);
		TransactionInfo restored = await _service.GetById(first);
		Assert.That(restored.Entries[0].AccountId, Is.EqualTo(_account.Id));
		Assert.That(restored.Entries[0].CumulativeAmount, Is.EqualTo(AppValues.MaxDecimal));
	}
	[Test]
	public async Task AccountTree_ReturnsLiveAccountsWithCurrencyNamesAndRootOnce()
	{
		Account removed = new() { Id = Guid.NewGuid(), Name = "Removed", GroupId = _account.GroupId, Group = _account.Group,
			CurrencyId = _account.CurrencyId, Currency = _account.Currency, DeleteRevision = 0 };
		_unitOfWork.AccountRepo.Add(removed);
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
		Business.Contracts.Services.Accounts.AccountTreeInfo tree = await _scope.ServiceProvider
			.GetRequiredService<IAccountGroupService>().GetAccountsTree();
		Assert.That(tree.Groups, Has.Count.EqualTo(1));
		Assert.That(tree.Groups[0].ParentId, Is.EqualTo(tree.Groups[0].Id));
		Assert.That(tree.Elements.Select(entry => entry.Id), Is.EqualTo(new[] { _account.Id, _other.Id, _destination.Id }));
		Assert.That(tree.Elements.All(entry => entry.CurrencyName == "Euro" && entry.CurrencyId == _account.CurrencyId), Is.True);
		Assert.That(_context.ChangeTracker.Entries(), Is.Empty);
	}

	[Test]
	public async Task NumericStorage_PersistsRatesAndTemplateAmountsAsExactScaledIntegers()
	{
		Guid transactionId = Guid.NewGuid();
		Seed(transactionId, _date, TransactionState.Draft, null, (_account, 12.3456m));
		_unitOfWork.CurrencyRateRepo.Add(new CurrencyRate { Id = Guid.NewGuid(), CurrencyId = _account.CurrencyId,
			Currency = _account.Currency, Date = new DateOnly(2025, 1, 1), Rate = 1.2345m });
		TemplateGroup group = new() { Id = Guid.NewGuid(), Name = "Templates" };
		group.ParentId = group.Id;
		group.Parent = group;
		Template template = new() { Id = Guid.NewGuid(), Name = "Test", GroupId = group.Id, Group = group };
		_unitOfWork.TemplateGroupRepo.Add(group);
		_unitOfWork.TemplateRepo.Add(template);
		Guid entryId = Guid.NewGuid();
		_unitOfWork.TemplateEntryRepo.Add(new TemplateEntry { Id = entryId, TemplateId = template.Id, Template = template,
			AccountId = _account.Id, Account = _account, Amount = -12.3456m });
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
		TransactionEntry entry = (await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(transactionId)).Single();
		entry.Rate = 1.2345m;
		_unitOfWork.TransactionEntryRepo.Update(entry);
		await _unitOfWork.SaveChangesAsync();
		_context.ChangeTracker.Clear();
		using System.Data.Common.DbCommand command = _context.Database.GetDbConnection().CreateCommand();
		command.CommandText = "SELECT Rate, typeof(Rate) FROM TransactionEntries UNION ALL SELECT Rate, typeof(Rate) FROM CurrencyRates UNION ALL SELECT Amount, typeof(Amount) FROM TemplateEntries";
		using System.Data.Common.DbDataReader reader = await command.ExecuteReaderAsync();
		List<long> stored = new();
		while (await reader.ReadAsync())
		{
			stored.Add(reader.GetInt64(0));
			Assert.That(reader.GetString(1), Is.EqualTo("integer"));
		}
		Assert.That(stored, Is.EqualTo(new long[] { 12345, 12345, -123456 }));
		Assert.That((await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(transactionId)).Single().Rate, Is.EqualTo(1.2345m));
		Assert.That((await _unitOfWork.CurrencyRateRepo.GetAllAsync()).Single().Rate, Is.EqualTo(1.2345m));
		Assert.That((await _unitOfWork.TemplateEntryRepo.GetByIdAsync(entryId))!.Amount, Is.EqualTo(-12.3456m));
	}
	private void StartNewScopeAfterRollback()
	{
		Assert.ThrowsAsync<ObjectDisposedException>(async () => await _context.SaveChangesAsync());
		_scope.Dispose();
		_scope = _provider.CreateScope();
		_context = _scope.ServiceProvider.GetRequiredService<AppDbContext>();
		_unitOfWork = _scope.ServiceProvider.GetRequiredService<IAppUnitOfWork>();
		_service = _scope.ServiceProvider.GetRequiredService<ITransactionService>();
		_cumulative = _scope.ServiceProvider.GetRequiredService<ICumulativeService>();
	}
	private TransactionParam Input(DateTime dateTime, decimal amount)
	{
		TransactionParam param = new() { DateTime = dateTime };
		param.Entries.Add(new TransactionEntryParam { AccountId = _account.Id, Amount = amount, Rate = 1m });
		param.Entries.Add(new TransactionEntryParam { AccountId = _other.Id, Amount = -amount, Rate = 1m });
		return param;
	}

	private void Seed(Guid id, DateTime dateTime, TransactionState state, long? deleted, params (Account Account, decimal Amount)[] entries)
	{
		Transaction entity = new() { Id = id, DateTime = dateTime, State = state, DeleteRevision = deleted, EditRevision = 5 };
		_unitOfWork.TransactionRepo.Add(entity);
		for (int index = 0; index < entries.Length; index++)
		{
			_unitOfWork.TransactionEntryRepo.Add(new TransactionEntry
			{
				Id = Guid.NewGuid(), TransactionId = id, Transaction = entity,
				AccountId = entries[index].Account.Id, Account = entries[index].Account,
				Amount = entries[index].Amount, Rate = 1m, Position = index, CumulativeAmount = 77m
			});
		}
	}
}
