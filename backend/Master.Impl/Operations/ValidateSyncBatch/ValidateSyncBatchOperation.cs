using Master.Impl.Operations.ApplySyncBatch;
using Business.Models.Entities.Reporting;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;
using Business.Models.Exceptions;
using Shared.Contracts;
using ConflictPriority = Exchange.Models.Enums.ConflictPriority;

namespace Master.Impl.Operations.ValidateSyncBatch;

/// <summary>
/// Validates the detached state selected by Master conflict resolution.
/// Checks accounting content after Master dependency restoration.
/// Keeps protected identities alive and preserves immutable currency relationships.
/// Repairs hierarchy cycles and unique names using the batch preference.
/// Normalizes independent catalog orders without changing content revisions.
/// Numeric validation uses configured precision and checked decimal arithmetic.
/// Corrections are logged; invalid references reject the entire batch.
/// This helper neither persists entities nor manages the caller's transaction.
/// </summary>
internal sealed class ValidateSyncBatchOperation : IValidateSyncBatchOperation
{
	private readonly ISharedContext _sharedContext;

	public ValidateSyncBatchOperation(ISharedContext sharedContext) => _sharedContext = sharedContext;

	public void Run(Dictionary<string, SyncEntityState> sets, SystemConfig config, ConflictPriority priority, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		if (config.AmountPrecision is < 0 or > AppValues.MaxPrecision || config.RatePrecision is < 0 or > AppValues.MaxPrecision || config.BaseCurrencyId == Guid.Empty)
		{
			throw new InvalidSyncBatchException("The Master precision or base-currency configuration is invalid.");
		}
		foreach (SyncEntityState set in sets.Values.Where(set => set.RootId.HasValue)) { RepairCycles(set, priority); }
		try { ValidateContent(sets, config, cancellationToken); }
		catch (OverflowException) { throw new InvalidSyncBatchException("A batch amount, rate, product or total exceeds the supported numeric range."); }
		foreach (SyncEntityState set in sets.Values)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IEnumerable<SyncMergeRow> members = set.Rows.Values.Where(row => row.IsLive && row.Entity.Id != set.RootId);
			if (set.ParentType is not null && set.Type is not (nameof(Account) or nameof(Report)))
			{
				foreach (IGrouping<Guid, SyncMergeRow> group in members.GroupBy(row => GetParentId(set, row)))
				{
					HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
					HashSet<string> reservedNames = group.Select(row => ((CatalogEntity)row.Entity).Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
					foreach (SyncMergeRow row in group.OrderByDescending(row => IsPreferred(row, priority)).ThenBy(row => row.Entity.Id.ToString("D"), StringComparer.Ordinal))
					{
						CatalogEntity entity = (CatalogEntity)row.Entity;
						string original = entity.Name;
						if (!names.Add(entity.Name))
						{
							do { entity.Name += "_1"; } while (!reservedNames.Add(entity.Name));
							names.Add(entity.Name);
						}
						if (entity.Name != original) { _sharedContext.LogService.Warn($"Renamed {set.Type}/{entity.Id} to resolve a sibling name collision."); }
					}
				}
			}
			if (set.IsOrdered)
			{
				foreach (IGrouping<Guid, SyncMergeRow> group in members.GroupBy(row => set.ParentType is null ? Guid.Empty : GetParentId(set, row)))
				{
					int order = 0;
					foreach (SyncMergeRow row in group.OrderBy(row => ((IOrderedEntity)row.Entity).Order)
						.ThenBy(row => row.Entity.Id.ToString("D"), StringComparer.Ordinal))
					{
						((IOrderedEntity)row.Entity).Order = order++;
					}
				}
			}
		}
	}

	private static bool IsPreferred(SyncMergeRow row, ConflictPriority priority) => priority == ConflictPriority.Local ? row.IncomingContent : !row.IncomingContent;

	private void RepairCycles(SyncEntityState set, ConflictPriority priority)
	{
		foreach (SyncMergeRow start in set.Rows.Values.Where(row => row.IsLive))
		{
			List<SyncMergeRow> path = new();
			Dictionary<Guid, int> positions = new();
			SyncMergeRow row = start;
			while (row.Entity.Id != set.RootId)
			{
				if (positions.TryGetValue(row.Entity.Id, out int cycleStart))
				{
					List<SyncMergeRow> cycle = path.Skip(cycleStart).ToList();
					SyncMergeRow move = cycle.OrderBy(row => IsPreferred(row, priority)).ThenByDescending(candidate => candidate.Entity.Id.ToString("D"), StringComparer.Ordinal).First();
					Guid oldParent = move.Original?[set.ParentField!].GetGuid() ?? set.RootId!.Value;
					Guid parent = CanReparent(set, move.Entity.Id, oldParent) ? oldParent : set.RootId!.Value;
					move.Entity.GetType().GetProperty(set.ParentField!)!.SetValue(move.Entity, parent);
					_sharedContext.LogService.Warn($"Moved {set.Type}/{move.Entity.Id} to {parent} to break a hierarchy cycle.");
					break;
				}
				positions.Add(row.Entity.Id, path.Count);
				path.Add(row);
				row = set.Rows[GetParentId(set, row)];
			}
		}
	}

	private static bool CanReparent(SyncEntityState set, Guid id, Guid parent)
	{
		HashSet<Guid> visited = [id];
		while (parent != set.RootId)
		{
			if (!visited.Add(parent) || !set.Rows.TryGetValue(parent, out SyncMergeRow? row) || !row.IsLive) { return false; }
			parent = GetParentId(set, row);
		}
		return true;
	}

	private void ValidateContent(Dictionary<string, SyncEntityState> sets, SystemConfig config, CancellationToken cancellationToken)
	{
		HashSet<Guid> transactionEntryIds = new();
		HashSet<Guid> templateEntryIds = new();
		HashSet<string> currencyCodes = new(StringComparer.OrdinalIgnoreCase);
		foreach (SyncEntityState set in sets.Values)
		{
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (set.ParentType is not null && !sets[set.ParentType].Rows.ContainsKey(GetParentId(set, row)))
				{
					throw new InvalidSyncBatchException($"{set.Type}/{row.Entity.Id} references a missing parent.");
				}
				if (row.Entity is Account account && (!sets[nameof(Currency)].Rows.ContainsKey(account.CurrencyId) ||
					(account.CategoryId is Guid categoryId && !sets[nameof(Category)].Rows.ContainsKey(categoryId)) ||
					(account.CorrespondentId is Guid correspondentId && !sets[nameof(Correspondent)].Rows.ContainsKey(correspondentId)) ||
					(account.ProjectId is Guid projectId && !sets[nameof(Project)].Rows.ContainsKey(projectId))))
				{
					throw new InvalidSyncBatchException($"Account {account.Id} references a missing currency or classification.");
				}
				if (row.Entity is Currency currency)
				{
					if (currency.Code.Length != AppValues.CurrencyCodeLength || currency.Code.Any(character => character is < 'A' or > 'Z') || currency.Code == MiskValues.NoCurrencyCode ||
						string.IsNullOrWhiteSpace(currency.Symbol) || string.IsNullOrWhiteSpace(currency.EnglishName) ||
						string.IsNullOrWhiteSpace(currency.Name) || currency.Name.Trim().Length > AppValues.MaxCurrencyNameLength || !currencyCodes.Add(currency.Code))
					{
						throw new InvalidSyncBatchException($"Currency {currency.Id} has invalid metadata or a conflicting permanent identity.");
					}
					if (row.Original is not null && row.Original[nameof(Currency.Code)].GetString() != currency.Code)
					{
						throw new InvalidSyncBatchException($"Currency {currency.Id} cannot change its ISO code.");
					}
					currency.Name = currency.Name.Trim();
				}
				if (row.Entity is CatalogEntity catalog)
				{
					if (string.IsNullOrWhiteSpace(catalog.Name)) { throw new InvalidSyncBatchException($"{set.Type}/{catalog.Id} requires a name."); }
					catalog.Name = catalog.Name.Trim();
				}
				switch (row.Entity)
				{
					case CurrencyRate rate:
						if (!sets[nameof(Currency)].Rows.ContainsKey(rate.CurrencyId) || (!rate.IsInitial && rate.Date < AppValues.MinDate))
						{
							throw new InvalidSyncBatchException($"Currency rate {rate.Id} has an invalid currency or date.");
						}
						rate.Rate = Normalize(rate.Rate, config.RatePrecision);
						ValidateRate(rate.Rate, rate.CurrencyId, config);
						break;
					case Transaction transaction:
						if (transaction.DateTime.Kind != DateTimeKind.Utc || transaction.DateTime < AppValues.MinDateTime ||
							transaction.State is not (TransactionState.Draft or TransactionState.Planned or TransactionState.Confirmed))
						{
							throw new InvalidSyncBatchException($"Transaction {transaction.Id} has an invalid date or state.");
						}
						decimal total = 0m;
						for (int position = 0; position < transaction.Entries.Count; position++)
						{
							TransactionEntry entry = transaction.Entries[position];
							ValidateEntry(sets, entry.Id, entry.AccountId, position, entry.Position, transactionEntryIds);
							entry.Amount = Normalize(entry.Amount, config.AmountPrecision);
							entry.Rate = Normalize(entry.Rate, config.RatePrecision);
							ValidateRate(entry.Rate, ((Account)sets[nameof(Account)].Rows[entry.AccountId].Entity).CurrencyId, config);
							total += Math.Round(entry.Amount * entry.Rate, config.AmountPrecision, MidpointRounding.ToEven);
						}
						if ((transaction.Entries.Count < AppValues.MinTransactionEntryCount || total != 0m) && transaction.State != TransactionState.Draft)
						{
							transaction.State = TransactionState.Draft;
							_sharedContext.LogService.Warn($"Transaction {transaction.Id} became Draft because its accepted entries do not balance.");
						}
						break;
					case Template template:
						for (int position = 0; position < template.Entries.Count; position++)
						{
							TemplateEntry entry = template.Entries[position];
							ValidateEntry(sets, entry.Id, entry.AccountId, position, entry.Position, templateEntryIds);
							entry.Amount = Normalize(entry.Amount, config.AmountPrecision);
						}
						break;
				}
			}
		}
	}

	private static void ValidateEntry(Dictionary<string, SyncEntityState> sets, Guid id, Guid accountId, int expectedPosition, int position, HashSet<Guid> ids)
	{
		if (id == Guid.Empty || !ids.Add(id) || position != expectedPosition || !sets[nameof(Account)].Rows.ContainsKey(accountId))
		{
			throw new InvalidSyncBatchException("Aggregate entry identities must be unique, positions consecutive and account references valid.");
		}
	}

	private static decimal Normalize(decimal value, int precision)
	{
		decimal normalized = Math.Round(value, precision, MidpointRounding.ToEven);
		if (normalized < AppValues.MinDecimal || normalized > AppValues.MaxDecimal)
		{
			throw new InvalidSyncBatchException("A normalized amount or rate cannot fit the signed 64-bit storage range.");
		}
		return normalized;
	}

	private static void ValidateRate(decimal rate, Guid currencyId, SystemConfig config)
	{
		if (rate <= 0 || (currencyId == config.BaseCurrencyId && rate != 1m))
		{
			throw new InvalidSyncBatchException("Rates must be positive; a base-currency rate must equal one.");
		}
	}

	private static Guid GetParentId(SyncEntityState set, SyncMergeRow row)
		=> (Guid)row.Entity.GetType().GetProperty(set.ParentField!)!.GetValue(row.Entity)!;
}
