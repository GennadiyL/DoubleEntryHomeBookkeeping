using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;
using Business.Models.Exceptions;
using Shared.Contracts;
using ConflictPriority = Exchange.Models.Enums.ConflictPriority;

namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Validates the detached state selected by Master conflict resolution.
/// Restores required dependencies before checking accepted accounting content.
/// Keeps protected identities alive and preserves immutable currency relationships.
/// Repairs hierarchy cycles and unique names using the batch preference.
/// Normalizes independent catalog orders without changing content revisions.
/// Numeric validation uses configured precision and checked decimal arithmetic.
/// Corrections are logged; invalid references reject the entire batch.
/// This helper neither persists entities nor manages the caller's transaction.
/// </summary>
internal sealed class SyncBatchValidation
{
	private readonly Dictionary<string, SyncEntitySet> _sets;
	private readonly SystemConfig _config;
	private readonly ILogService _log;
	private readonly ConflictPriority _priority;
	private readonly Queue<(SyncEntitySet Set, SyncMergeRow Row)> _required = new();
	private readonly HashSet<(string Type, Guid Id)> _visited = new();

	public SyncBatchValidation(Dictionary<string, SyncEntitySet> sets, SystemConfig config, ILogService log, ConflictPriority priority)
	{
		_sets = sets;
		_config = config;
		_log = log;
		_priority = priority;
	}

	public void Run(CancellationToken cancellationToken)
	{
		if (_config.AmountPrecision is < 0 or > 4 || _config.RatePrecision is < 0 or > 4 || _config.BaseCurrencyId == Guid.Empty)
		{
			throw new InvalidSyncBatchException("The Master precision or base-currency configuration is invalid.");
		}
		foreach (SyncEntitySet set in _sets.Values)
		{
			if (set.RootId is Guid rootId)
			{
				SyncMergeRow root = Require(set.Type, rootId);
				set.SetParentId(root, rootId);
				((CatalogEntity)root.Entity).IsFavorite = false;
			}
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				if (row.Entity is Account account && row.Original is not null)
				{
					Guid originalCurrency = row.Original["currencyId"].GetGuid();
					if (account.CurrencyId != originalCurrency)
					{
						account.CurrencyId = originalCurrency;
						_log.Warn($"Preserved immutable currency of account {account.Id}.");
					}
				}
				if (row.IsLive) { _required.Enqueue((set, row)); }
			}
		}
		Require("currency", _config.BaseCurrencyId);
		Account balancing = (Account)Require("account", EntitiesIds.BalancingAccountId).Entity;
		if (balancing.CurrencyId != _config.BaseCurrencyId)
		{
			throw new InvalidSyncBatchException("The permanent balancing account must use the base currency.");
		}
		while (_required.TryDequeue(out (SyncEntitySet Set, SyncMergeRow Row) item))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!_visited.Add((item.Set.Type, item.Row.Entity.Id))) { continue; }
			if (item.Set.ParentType is not null)
			{
				Require(item.Set.ParentType, item.Set.ParentId(item.Row));
			}
			switch (item.Row.Entity)
			{
				case Account account:
					Require("currency", account.CurrencyId);
					if (account.CategoryId is Guid categoryId) { Require("category", categoryId); }
					if (account.CorrespondentId is Guid correspondentId) { Require("correspondent", correspondentId); }
					if (account.ProjectId is Guid projectId) { Require("project", projectId); }
					break;
				case Transaction transaction:
					foreach (TransactionEntry entry in transaction.Entries) { Require("account", entry.AccountId); }
					break;
				case Template template:
					foreach (TemplateEntry entry in template.Entries) { Require("account", entry.AccountId); }
					break;
				case Currency currency:
					SyncMergeRow[] initial = _sets["currencyRate"].Rows.Values.Where(row =>
						row.Entity is CurrencyRate rate && rate.CurrencyId == currency.Id && rate.IsInitial).ToArray();
					if (initial.Length != 1)
					{
						throw new InvalidSyncBatchException($"Currency {currency.Id} requires exactly one initial rate.");
					}
					Require("currencyRate", initial[0].Entity.Id);
					break;
				case CurrencyRate rate:
					if (!_sets["currency"].Rows.ContainsKey(rate.CurrencyId))
					{
						throw new InvalidSyncBatchException($"Currency rate {rate.Id} references a missing currency.");
					}
					if (item.Row.IncomingContent) { Require("currency", rate.CurrencyId); }
					break;
			}
		}
		foreach (SyncEntitySet set in _sets.Values.Where(set => set.RootId.HasValue)) { RepairCycles(set); }
		try { ValidateContent(cancellationToken); }
		catch (OverflowException) { throw new InvalidSyncBatchException("A batch amount, rate, product or total exceeds the supported numeric range."); }
		foreach (SyncEntitySet set in _sets.Values)
		{
			cancellationToken.ThrowIfCancellationRequested();
			IEnumerable<SyncMergeRow> members = set.Rows.Values.Where(row => row.IsLive && row.Entity.Id != set.RootId);
			if (set.ParentType is not null && set.Type is not ("account" or "report"))
			{
				foreach (IGrouping<Guid, SyncMergeRow> group in members.GroupBy(set.ParentId))
				{
					HashSet<string> names = new(StringComparer.OrdinalIgnoreCase);
					HashSet<string> reservedNames = group.Select(row => ((CatalogEntity)row.Entity).Name).ToHashSet(StringComparer.OrdinalIgnoreCase);
					foreach (SyncMergeRow row in group.OrderByDescending(IsPreferred).ThenBy(row => row.Entity.Id.ToString("D"), StringComparer.Ordinal))
					{
						CatalogEntity entity = (CatalogEntity)row.Entity;
						string original = entity.Name;
						if (!names.Add(entity.Name))
						{
							do { entity.Name += "_1"; } while (!reservedNames.Add(entity.Name));
							names.Add(entity.Name);
						}
						if (entity.Name != original) { _log.Warn($"Renamed {set.Type}/{entity.Id} to resolve a sibling name collision."); }
					}
				}
			}
			if (set.IsOrdered)
			{
				foreach (IGrouping<Guid, SyncMergeRow> group in members.GroupBy(row => set.ParentType is null ? Guid.Empty : set.ParentId(row)))
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

	private SyncMergeRow Require(string type, Guid id)
	{
		SyncEntitySet set = _sets[type];
		if (!set.Rows.TryGetValue(id, out SyncMergeRow? row))
		{
			throw new InvalidSyncBatchException($"Required {type}/{id} is missing. Synchronize its creation before dependent changes.");
		}
		if (!row.IsLive)
		{
			row.Tracking.DeleteRevision = null;
			_log.Warn($"Restored required {type}/{id} during batch reconciliation.");
		}
		_required.Enqueue((set, row));
		return row;
	}

	private bool IsPreferred(SyncMergeRow row) => _priority == ConflictPriority.Local ? row.IncomingContent : !row.IncomingContent;

	private void RepairCycles(SyncEntitySet set)
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
					SyncMergeRow move = cycle.OrderBy(IsPreferred).ThenByDescending(candidate => candidate.Entity.Id.ToString("D"), StringComparer.Ordinal).First();
					Guid oldParent = move.Original?["parentId"].GetGuid() ?? set.RootId!.Value;
					Guid parent = CanReparent(set, move.Entity.Id, oldParent) ? oldParent : set.RootId!.Value;
					set.SetParentId(move, parent);
					_log.Warn($"Moved {set.Type}/{move.Entity.Id} to {parent} to break a hierarchy cycle.");
					break;
				}
				positions.Add(row.Entity.Id, path.Count);
				path.Add(row);
				row = set.Rows[set.ParentId(row)];
			}
		}
	}

	private static bool CanReparent(SyncEntitySet set, Guid id, Guid parent)
	{
		HashSet<Guid> visited = [id];
		while (parent != set.RootId)
		{
			if (!visited.Add(parent) || !set.Rows.TryGetValue(parent, out SyncMergeRow? row) || !row.IsLive) { return false; }
			parent = set.ParentId(row);
		}
		return true;
	}

	private void ValidateContent(CancellationToken cancellationToken)
	{
		HashSet<Guid> transactionEntryIds = new();
		HashSet<Guid> templateEntryIds = new();
		HashSet<string> currencyCodes = new(StringComparer.OrdinalIgnoreCase);
		foreach (SyncEntitySet set in _sets.Values)
		{
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				cancellationToken.ThrowIfCancellationRequested();
				if (set.ParentType is not null && !_sets[set.ParentType].Rows.ContainsKey(set.ParentId(row)))
				{
					throw new InvalidSyncBatchException($"{set.Type}/{row.Entity.Id} references a missing parent.");
				}
				if (row.Entity is Account account && (!_sets["currency"].Rows.ContainsKey(account.CurrencyId) ||
					(account.CategoryId is Guid categoryId && !_sets["category"].Rows.ContainsKey(categoryId)) ||
					(account.CorrespondentId is Guid correspondentId && !_sets["correspondent"].Rows.ContainsKey(correspondentId)) ||
					(account.ProjectId is Guid projectId && !_sets["project"].Rows.ContainsKey(projectId))))
				{
					throw new InvalidSyncBatchException($"Account {account.Id} references a missing currency or classification.");
				}
				if (row.Entity is Currency currency)
				{
					if (currency.Code.Length != 3 || currency.Code.Any(character => character is < 'A' or > 'Z') || currency.Code == "XXX" ||
						string.IsNullOrWhiteSpace(currency.Symbol) || string.IsNullOrWhiteSpace(currency.EnglishName) ||
						string.IsNullOrWhiteSpace(currency.Name) || currency.Name.Trim().Length > 6 || !currencyCodes.Add(currency.Code))
					{
						throw new InvalidSyncBatchException($"Currency {currency.Id} has invalid metadata or a conflicting permanent identity.");
					}
					if (row.Original is not null && row.Original["code"].GetString() != currency.Code)
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
						if (!_sets["currency"].Rows.ContainsKey(rate.CurrencyId) || (!rate.IsInitial && rate.Date < AppValues.MinDate))
						{
							throw new InvalidSyncBatchException($"Currency rate {rate.Id} has an invalid currency or date.");
						}
						rate.Rate = Normalize(rate.Rate, _config.RatePrecision);
						ValidateRate(rate.Rate, rate.CurrencyId);
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
							ValidateEntry(entry.Id, entry.AccountId, position, entry.Position, transactionEntryIds);
							entry.Amount = Normalize(entry.Amount, _config.AmountPrecision);
							entry.Rate = Normalize(entry.Rate, _config.RatePrecision);
							ValidateRate(entry.Rate, ((Account)_sets["account"].Rows[entry.AccountId].Entity).CurrencyId);
							total += Math.Round(entry.Amount * entry.Rate, _config.AmountPrecision, MidpointRounding.ToEven);
						}
						if ((transaction.Entries.Count < 2 || total != 0m) && transaction.State != TransactionState.Draft)
						{
							transaction.State = TransactionState.Draft;
							_log.Warn($"Transaction {transaction.Id} became Draft because its accepted entries do not balance.");
						}
						break;
					case Template template:
						for (int position = 0; position < template.Entries.Count; position++)
						{
							TemplateEntry entry = template.Entries[position];
							ValidateEntry(entry.Id, entry.AccountId, position, entry.Position, templateEntryIds);
							entry.Amount = Normalize(entry.Amount, _config.AmountPrecision);
						}
						break;
				}
			}
		}
	}

	private void ValidateEntry(Guid id, Guid accountId, int expectedPosition, int position, HashSet<Guid> ids)
	{
		if (id == Guid.Empty || !ids.Add(id) || position != expectedPosition || !_sets["account"].Rows.ContainsKey(accountId))
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

	private void ValidateRate(decimal rate, Guid currencyId)
	{
		if (rate <= 0 || (currencyId == _config.BaseCurrencyId && rate != 1m))
		{
			throw new InvalidSyncBatchException("Rates must be positive; a base-currency rate must equal one.");
		}
	}
}
