using Business.Contracts.Services;
using Business.Contracts.Services.Transactions;
using Business.Contracts.Utils.Merging;
using Business.Contracts.Utils.Models;
using Business.Impl.Operations.Config;
using Business.Impl.Operations.Cumulative;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Queries;
using DataAccess.Core.Entities;
using Shared.Contracts;

namespace Business.Impl.Services;

/// <summary>
/// Implements atomic transaction mutations and detached transaction reads.
/// Validates entries and derives Draft or Confirmed state on every save.
/// Replaces aggregate entries while preserving the transaction identity.
/// Owns database transactions around mutations and cumulative maintenance.
/// Navigation returns the newest bounded set through a selected local date.
/// Refresh preserves displayed identities without filling removed rows.
/// Balances include only live Confirmed contributions at stored rates.
/// Errors roll back the complete mutation without automatic retry.
/// </summary>
internal sealed class TransactionService : ITransactionService
{
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IConfigOperation _configOperation;
	private readonly ICumulativeOperation _cumulativeOperation;

	public TransactionService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork,
		IConfigOperation configOperation, ICumulativeOperation cumulativeOperation)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_configOperation = configOperation;
		_cumulativeOperation = cumulativeOperation;
	}

	public Task<Guid> Add(TransactionParam param, CancellationToken cancellationToken = default) =>
		SaveTransaction(null, param, cancellationToken);

	public async Task Update(Guid entityId, TransactionParam param, CancellationToken cancellationToken = default) =>
		await SaveTransaction(entityId, param, cancellationToken);

	private async Task<Guid> SaveTransaction(Guid? entityId, TransactionParam param, CancellationToken cancellationToken)
	{
		if (param is null || param.DateTime.Kind != DateTimeKind.Utc || param.DateTime < AppValues.MinDateTime)
		{
			throw new InvalidElementException("A valid UTC transaction timestamp is required.");
		}
		IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
		try
		{
			Transaction entity = entityId.HasValue
				? await GetActiveTransaction(entityId.Value, cancellationToken)
				: new Transaction { Id = Guid.NewGuid() };
			DateTime fromDateTime = entityId.HasValue && entity.DateTime < param.DateTime ? entity.DateTime : param.DateTime;
			ICollection<TransactionEntry> oldEntries = entityId.HasValue
				? await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(entity.Id, cancellationToken)
				: new List<TransactionEntry>();
			SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
			List<TransactionEntry> entries = [];
			decimal baseTotal = 0m;
			foreach (TransactionEntryParam input in param.Entries)
			{
				if (input is null)
				{
					throw new InvalidElementException("Every entry requires valid account, amount and rate values.");
				}
				Account? account = await _unitOfWork.AccountRepo.GetByIdAsync(input.AccountId, cancellationToken);
				if (account is null || account.IsDeleted())
				{
					throw new InvalidElementException("The entry account does not exist or is deleted.");
				}
				decimal amount = Normalize(input.Amount, config.AmountPrecision);
				decimal rate = Normalize(input.Rate, config.RatePrecision);
				if (rate <= 0 || account.CurrencyId == config.BaseCurrencyId && rate != 1m)
				{
					throw new InvalidElementException("A positive rate is required; the base-currency rate must be one.");
				}
				TransactionEntry entry = new()
				{
					Id = Guid.NewGuid(), TransactionId = entity.Id, Transaction = entity,
					AccountId = account.Id, Account = account, Position = entries.Count,
					Amount = amount, Rate = rate, CumulativeAmount = 0m
				};
				baseTotal += entry.GetBaseAmount(config.AmountPrecision);
				entries.Add(entry);
			}
			HashSet<Guid> accounts = [.. oldEntries.Select(entry => entry.AccountId), .. entries.Select(entry => entry.AccountId)];
			entity.DateTime = param.DateTime;
			entity.Description = param.Description;
			entity.State = entries.Count >= 2 && baseTotal == 0m ? TransactionState.Confirmed : TransactionState.Draft;
			entity.Entries = entries;
			if (entityId.HasValue)
			{
				entity.SetEditedContent();
				await _unitOfWork.TransactionEntryRepo.RemoveRangeAsync(oldEntries, cancellationToken);
				_unitOfWork.TransactionRepo.Update(entity);
			}
			else
			{
				_unitOfWork.TransactionRepo.Add(entity);
			}
			foreach (TransactionEntry entry in entries)
			{
				_unitOfWork.TransactionEntryRepo.Add(entry);
			}
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			foreach (Guid accountId in accounts)
			{
				await _cumulativeOperation.RecalculateAsync(accountId, fromDateTime, cancellationToken);
			}
			await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
			return entity.Id;
		}
		catch
		{
			await _unitOfWork.RollbackTransactionAsync(transaction, CancellationToken.None);
			throw;
		}
	}

	public async Task Delete(Guid entityId, CancellationToken cancellationToken = default)
	{
		IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
		try
		{
			Transaction entity = await GetActiveTransaction(entityId, cancellationToken);
			ICollection<TransactionEntry> entries = await _unitOfWork.TransactionEntryRepo.GetByTransactionIdAsync(entity.Id, cancellationToken);
			entity.SetDeleted();
			_unitOfWork.TransactionRepo.Update(entity);
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			foreach (Guid accountId in entries.Select(entry => entry.AccountId).Distinct())
			{
				await _cumulativeOperation.RecalculateAsync(accountId, entity.DateTime, cancellationToken);
			}
			await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
		}
		catch
		{
			await _unitOfWork.RollbackTransactionAsync(transaction, CancellationToken.None);
			throw;
		}
	}

	public Task DeleteTransactions(DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
		DeleteTransactions(new TransactionSearch(), fromDate, toDate, cancellationToken);

	public Task DeleteTransactionsByAccount(Guid accountId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
		DeleteTransactions(new TransactionSearch { AccountId = accountId }, fromDate, toDate, cancellationToken);

	public Task DeleteTransactionsByCategory(Guid categoryId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
		DeleteTransactions(new TransactionSearch { CategoryId = categoryId }, fromDate, toDate, cancellationToken);

	public Task DeleteTransactionsByCorrespondent(Guid correspondentId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
		DeleteTransactions(new TransactionSearch { CorrespondentId = correspondentId }, fromDate, toDate, cancellationToken);

	public Task DeleteTransactionsByProject(Guid projectId, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken = default) =>
		DeleteTransactions(new TransactionSearch { ProjectId = projectId }, fromDate, toDate, cancellationToken);

	private async Task DeleteTransactions(TransactionSearch search, DateOnly fromDate, DateOnly toDate, CancellationToken cancellationToken)
	{
		if (fromDate > toDate)
		{
			throw new InvalidElementException("The beginning of the range must not follow its end.");
		}
		search = search with { FromDateTime = StartOfDay(fromDate), BeforeDateTime = EndOfDay(toDate) };
		IUnitOfWorkTransaction transaction = await _unitOfWork.BeginTransactionAsync(cancellationToken);
		try
		{
			await ValidateFilter(search, cancellationToken);
			ICollection<Transaction> matches = await _unitOfWork.TransactionRepo.SearchAsync(search, cancellationToken);
			if (matches.Count == 0)
			{
				await _unitOfWork.RollbackTransactionAsync(transaction, CancellationToken.None);
				return;
			}
			Dictionary<Guid, DateTime> affected = new();
			foreach (Transaction entity in matches)
			{
				foreach (TransactionEntry entry in entity.Entries)
				{
					if (!affected.TryGetValue(entry.AccountId, out DateTime boundary) || entity.DateTime < boundary)
					{
						affected[entry.AccountId] = entity.DateTime;
					}
				}
				entity.SetDeleted();
				_unitOfWork.TransactionRepo.Update(entity);
			}
			await _unitOfWork.SaveChangesAsync(cancellationToken);
			foreach (KeyValuePair<Guid, DateTime> account in affected)
			{
				await _cumulativeOperation.RecalculateAsync(account.Key, account.Value, cancellationToken);
			}
			await _unitOfWork.CommitTransactionAsync(transaction, cancellationToken);
		}
		catch
		{
			await _unitOfWork.RollbackTransactionAsync(transaction, CancellationToken.None);
			throw;
		}
	}

	public Task<TransactionListInfo> GetTransactions(DateOnly date, CancellationToken cancellationToken = default) =>
		GetTransactions(new TransactionSearch(), date, cancellationToken);

	public Task<TransactionListInfo> GetTransactionsByAccount(Guid accountId, DateOnly date, CancellationToken cancellationToken = default) =>
		GetTransactions(new TransactionSearch { AccountId = accountId }, date, cancellationToken);

	public Task<TransactionListInfo> GetTransactionsByCategory(Guid categoryId, DateOnly date, CancellationToken cancellationToken = default) =>
		GetTransactions(new TransactionSearch { CategoryId = categoryId }, date, cancellationToken);

	public Task<TransactionListInfo> GetTransactionsByCorrespondent(Guid correspondentId, DateOnly date, CancellationToken cancellationToken = default) =>
		GetTransactions(new TransactionSearch { CorrespondentId = correspondentId }, date, cancellationToken);

	public Task<TransactionListInfo> GetTransactionsByProject(Guid projectId, DateOnly date, CancellationToken cancellationToken = default) =>
		GetTransactions(new TransactionSearch { ProjectId = projectId }, date, cancellationToken);

	private async Task<TransactionListInfo> GetTransactions(TransactionSearch search, DateOnly date, CancellationToken cancellationToken)
	{
		await ValidateFilter(search, cancellationToken);
		search = search with { BeforeDateTime = EndOfDay(date), MaximumCount = AppValues.MaxTransactionListCount + 1 };
		ICollection<Transaction> entities = await _unitOfWork.TransactionRepo.SearchAsync(search, cancellationToken);
		TransactionListInfo result = new() { LimitExceeded = entities.Count > AppValues.MaxTransactionListCount };
		result.Transactions.AddRange(entities.Take(AppValues.MaxTransactionListCount).Select(ToInfo));
		return result;
	}

	public async Task<TransactionRefreshInfo> RefreshTransactions(TransactionRefreshParam param, CancellationToken cancellationToken = default)
	{
		if (param is null || param.TransactionIds.Count > AppValues.MaxTransactionListCount ||
			param.TransactionIds.Any(id => id == Guid.Empty) ||
			new[] { param.AccountId, param.CategoryId, param.CorrespondentId, param.ProjectId }.Count(id => id.HasValue) > 1)
		{
			throw new InvalidElementException("A valid displayed selection and at most one filter are required.");
		}
		TransactionSearch search = new()
		{
			BeforeDateTime = EndOfDay(param.Date),
			Ids = [.. param.TransactionIds.Distinct()],
			AccountId = param.AccountId, CategoryId = param.CategoryId,
			CorrespondentId = param.CorrespondentId, ProjectId = param.ProjectId
		};
		await ValidateFilter(search, cancellationToken);
		TransactionRefreshInfo result = new();
		if (search.Ids.Count == 0)
		{
			return result;
		}
		ICollection<Transaction> entities = await _unitOfWork.TransactionRepo.SearchAsync(search, cancellationToken);
		result.Transactions.AddRange(entities.Select(ToInfo));
		HashSet<Guid> retained = [.. entities.Select(entity => entity.Id)];
		result.RemovedTransactionIds.AddRange(search.Ids.Where(id => !retained.Contains(id)));
		return result;
	}

	public async Task<TransactionInfo> GetById(Guid id, CancellationToken cancellationToken = default)
	{
		ICollection<Transaction> matches = await _unitOfWork.TransactionRepo.SearchAsync(
			new TransactionSearch { Ids = [id] }, cancellationToken);
		Transaction? entity = matches.SingleOrDefault();
		if (entity is null)
		{
			throw new ElementNotFoundException("The transaction does not exist or is deleted.");
		}
		return ToInfo(entity);
	}

	public async Task<DuplicateTransactionInfo> DuplicateTransaction(Guid transactionId, CancellationToken cancellationToken = default)
	{
		TransactionInfo source = await GetById(transactionId, cancellationToken);
		DuplicateTransactionInfo result = new() { DateTime = _sharedContext.DateTimeService.UtcNow, Description = source.Description };
		result.Entries.AddRange(source.Entries.Select(entry => entry with { CumulativeAmount = 0m }));
		return result;
	}

	public async Task<List<AccountBalanceInfo>> GetBalancesForAllAccounts(DateOnly date, CancellationToken cancellationToken = default)
	{
		ICollection<Account> accounts = await _unitOfWork.AccountRepo.GetAllAsync(cancellationToken);
		return await GetBalances(accounts.Where(account => !account.IsDeleted()).ToList(), date, null, cancellationToken);
	}

	public async Task<AccountBalanceInfo> GetBalanceForAccount(Guid accountId, DateOnly date, CancellationToken cancellationToken = default)
	{
		Account? account = await _unitOfWork.AccountRepo.GetByIdAsync(accountId, cancellationToken);
		if (account is null || account.IsDeleted())
		{
			throw new ElementNotFoundException("The account does not exist or is deleted.");
		}
		return (await GetBalances([account], date, accountId, cancellationToken)).Single();
	}

	private async Task<List<AccountBalanceInfo>> GetBalances(List<Account> accounts, DateOnly date, Guid? accountId, CancellationToken cancellationToken)
	{
		SystemConfig config = await _configOperation.GetSystemConfig(cancellationToken);
		ICollection<Transaction> entities = await _unitOfWork.TransactionRepo.SearchAsync(
			new TransactionSearch { BeforeDateTime = EndOfDay(date), AccountId = accountId, State = TransactionState.Confirmed },
			cancellationToken);
		Dictionary<Guid, AccountBalanceInfo> balances = accounts.ToDictionary(account => account.Id,
			account => new AccountBalanceInfo { AccountId = account.Id, CurrencyId = account.CurrencyId });
		HashSet<Guid> initialized = [];
		foreach (Transaction entity in entities)
		{
			foreach (TransactionEntry entry in entity.Entries.OrderByDescending(entry => entry.Position))
			{
				if (!balances.TryGetValue(entry.AccountId, out AccountBalanceInfo? balance))
				{
					continue;
				}
				if (initialized.Add(entry.AccountId))
				{
					balance.Amount = entry.CumulativeAmount;
				}
				balance.BaseAmount += entry.GetBaseAmount(config.AmountPrecision);
			}
		}
		return [.. balances.Values];
	}

	private async Task ValidateFilter(TransactionSearch search, CancellationToken cancellationToken)
	{
		bool valid = true;
		if (search.AccountId.HasValue)
		{
			Account? entity = await _unitOfWork.AccountRepo.GetByIdAsync(search.AccountId.Value, cancellationToken);
			valid = entity is not null && !entity.IsDeleted();
		}
		if (search.CategoryId.HasValue)
		{
			Category? entity = await _unitOfWork.CategoryRepo.GetByIdAsync(search.CategoryId.Value, cancellationToken);
			valid &= entity is not null && !entity.IsDeleted();
		}
		if (search.CorrespondentId.HasValue)
		{
			Correspondent? entity = await _unitOfWork.CorrespondentRepo.GetByIdAsync(search.CorrespondentId.Value, cancellationToken);
			valid &= entity is not null && !entity.IsDeleted();
		}
		if (search.ProjectId.HasValue)
		{
			Project? entity = await _unitOfWork.ProjectRepo.GetByIdAsync(search.ProjectId.Value, cancellationToken);
			valid &= entity is not null && !entity.IsDeleted();
		}
		if (!valid)
		{
			throw new InvalidElementException("The selected filter identity does not exist or is deleted.");
		}
	}

	private async Task<Transaction> GetActiveTransaction(Guid id, CancellationToken cancellationToken)
	{
		Transaction? entity = await _unitOfWork.TransactionRepo.GetByIdAsync(id, cancellationToken);
		if (entity is null || entity.IsDeleted())
		{
			throw new ElementNotFoundException("The transaction does not exist or is deleted.");
		}
		return entity;
	}

	private static decimal Normalize(decimal value, int precision)
	{
		decimal normalized = Math.Round(value, precision, MidpointRounding.ToEven);
		if (normalized < AppValues.MinDecimal || normalized > AppValues.MaxDecimal)
		{
			throw new InvalidElementException("The numeric value exceeds the supported storage range.");
		}
		return normalized;
	}

	private static DateTime StartOfDay(DateOnly date) =>
		TimeZoneInfo.ConvertTimeToUtc(date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), TimeZoneInfo.Local);

	private static DateTime EndOfDay(DateOnly date)
	{
		if (date == DateOnly.MaxValue)
		{
			throw new InvalidElementException("The selected date must have a representable following day.");
		}
		return StartOfDay(date.AddDays(1));
	}

	private static TransactionInfo ToInfo(Transaction entity)
	{
		TransactionInfo result = new()
		{
			Id = entity.Id, DateTime = DateTime.SpecifyKind(entity.DateTime, DateTimeKind.Utc),
			State = entity.State, Description = entity.Description
		};
		foreach (TransactionEntry entry in entity.Entries.OrderBy(entry => entry.Position))
		{
			result.Entries.Add(new TransactionEntryInfo
			{
				AccountId = entry.AccountId, AccountName = entry.Account.Name,
				CurrencyId = entry.Account.CurrencyId, CurrencyName = entry.Account.Currency.Name,
				Amount = entry.Amount, CumulativeAmount = entry.CumulativeAmount, Rate = entry.Rate
			});
		}
		return result;
	}
}
