using Master.Impl.Operations.ApplySyncBatch;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Config;
using Business.Models.Exceptions;
using Shared.Contracts;

namespace Master.Impl.Operations.RestoreConsistency;

/// <summary>
/// Restores dependencies in the detached Master graph after batch merge.
/// Every live dependent requires a live referenced row, including unchanged rates.
/// A work queue traverses restored ancestors until all live references are resolved.
/// Restoring a Currency restores every retained rate in its aggregate.
/// Protected roots, base currency and balancing account remain alive.
/// Missing identities reject the batch because their content cannot be reconstructed.
/// Immutable account currency references are preserved before traversal.
/// The caller validates content and stamps restored rows with the batch revision.
/// Persistence shares the batch transaction and precedes snapshot publication.
/// Invocation state is local; this operation never changes Local or Candidate databases.
/// </summary>
internal sealed class RestoreConsistencyOperation : IRestoreConsistencyOperation
{
	private readonly ISharedContext _sharedContext;

	public RestoreConsistencyOperation(ISharedContext sharedContext) => _sharedContext = sharedContext;

	public void Run(Dictionary<string, SyncEntityState> sets, SystemConfig config, CancellationToken cancellationToken = default)
	{
		Queue<(SyncEntityState Set, SyncMergeRow Row)> required = new();
		HashSet<(string Type, Guid Id)> visited = new();
		HashSet<Guid> restoredCurrencies = new();
		cancellationToken.ThrowIfCancellationRequested();
		foreach (SyncEntityState set in sets.Values)
		{
			if (set.RootId is Guid rootId)
			{
				SyncMergeRow root = Require(sets, required, restoredCurrencies, set.Type, rootId);
				root.Entity.GetType().GetProperty(set.ParentField!)!.SetValue(root.Entity, rootId);
				((CatalogEntity)root.Entity).IsFavorite = false;
			}
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				if (row.Entity is Account account && row.Original is not null)
				{
					Guid originalCurrency = row.Original[nameof(Account.CurrencyId)].GetGuid();
					if (account.CurrencyId != originalCurrency)
					{
						account.CurrencyId = originalCurrency;
						_sharedContext.LogService.Warn($"Preserved immutable currency of account {account.Id}.");
					}
				}
				if (row.IsLive) { required.Enqueue((set, row)); }
			}
		}
		Require(sets, required, restoredCurrencies, nameof(Currency), config.BaseCurrencyId);
		Account balancing = (Account)Require(sets, required, restoredCurrencies, nameof(Account), EntitiesIds.BalancingAccountId).Entity;
		if (balancing.CurrencyId != config.BaseCurrencyId)
		{
			throw new InvalidSyncBatchException("The permanent balancing account must use the base currency.");
		}
		while (required.TryDequeue(out (SyncEntityState Set, SyncMergeRow Row) item))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (!visited.Add((item.Set.Type, item.Row.Entity.Id))) { continue; }
			if (item.Set.ParentType is not null)
			{
				Require(sets, required, restoredCurrencies, item.Set.ParentType, (Guid)item.Row.Entity.GetType().GetProperty(item.Set.ParentField!)!.GetValue(item.Row.Entity)!);
			}
			switch (item.Row.Entity)
			{
				case Account account:
					Require(sets, required, restoredCurrencies, nameof(Currency), account.CurrencyId);
					if (account.CategoryId is Guid categoryId) { Require(sets, required, restoredCurrencies, nameof(Category), categoryId); }
					if (account.CorrespondentId is Guid correspondentId) { Require(sets, required, restoredCurrencies, nameof(Correspondent), correspondentId); }
					if (account.ProjectId is Guid projectId) { Require(sets, required, restoredCurrencies, nameof(Project), projectId); }
					break;
				case Transaction transaction:
					foreach (TransactionEntry entry in transaction.Entries) { Require(sets, required, restoredCurrencies, nameof(Account), entry.AccountId); }
					break;
				case Template template:
					foreach (TemplateEntry entry in template.Entries) { Require(sets, required, restoredCurrencies, nameof(Account), entry.AccountId); }
					break;
				case Currency currency:
					if (item.Row.OriginalDeleteRevision is not null || restoredCurrencies.Contains(currency.Id))
					{
						foreach (SyncMergeRow retained in sets[nameof(CurrencyRate)].Rows.Values.Where(row =>
							((CurrencyRate)row.Entity).CurrencyId == currency.Id))
						{
							Require(sets, required, restoredCurrencies, nameof(CurrencyRate), retained.Entity.Id);
						}
					}
					SyncMergeRow[] initial = sets[nameof(CurrencyRate)].Rows.Values.Where(row =>
						row.Entity is CurrencyRate rate && rate.CurrencyId == currency.Id && rate.IsInitial).ToArray();
					if (initial.Length != 1)
					{
						throw new InvalidSyncBatchException($"Currency {currency.Id} requires exactly one initial rate.");
					}
					Require(sets, required, restoredCurrencies, nameof(CurrencyRate), initial[0].Entity.Id);
					break;
				case CurrencyRate rate:
					if (!sets[nameof(Currency)].Rows.ContainsKey(rate.CurrencyId))
					{
						throw new InvalidSyncBatchException($"Currency rate {rate.Id} references a missing currency.");
					}
					Require(sets, required, restoredCurrencies, nameof(Currency), rate.CurrencyId);
					break;
			}
		}
	}

	private SyncMergeRow Require(Dictionary<string, SyncEntityState> sets, Queue<(SyncEntityState Set, SyncMergeRow Row)> required, HashSet<Guid> restoredCurrencies, string type, Guid id)
	{
		SyncEntityState set = sets[type];
		if (!set.Rows.TryGetValue(id, out SyncMergeRow? row))
		{
			throw new InvalidSyncBatchException($"Required {type}/{id} is missing. Synchronize its creation before dependent changes.");
		}
		if (!row.IsLive)
		{
			if (type == nameof(Currency))
			{
				restoredCurrencies.Add(id);
			}
			row.Tracking.DeleteRevision = null;
			_sharedContext.LogService.Warn($"Restored required {type}/{id} during batch reconciliation.");
		}
		required.Enqueue((set, row));
		return row;
	}

}
