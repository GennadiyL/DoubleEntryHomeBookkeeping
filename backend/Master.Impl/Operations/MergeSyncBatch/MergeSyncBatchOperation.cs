using Master.Impl.Operations.ApplySyncBatch;
using Master.Impl.Operations.MapSyncEntity;
using Business.Models.Entities.Base;
using Business.Core.Entities;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using Exchange.Models.Enums;
using Exchange.Models.Synchronizations;
using Shared.Contracts;

namespace Master.Impl.Operations.MergeSyncBatch;

/// <summary>
/// Reconciles incoming objects with the detached state of one Master batch.
/// Every invocation owns its duplicate-detection indexes in a data-only workspace.
/// Currency rates match by currency and date while preserving Master identity.
/// Whole-entity content conflicts use revisions, priority and Local provenance.
/// Ordering is applied independently to live rows after content resolution.
/// Protected roots are logged and preserved without incoming changes.
/// Resulting graph validation and revision stamping remain caller responsibilities.
/// This helper does not access repositories, save changes or own transactions.
/// </summary>
internal sealed class MergeSyncBatchOperation : IMergeSyncBatchOperation
{
	private readonly ISharedContext _sharedContext;
	private readonly IMapSyncEntityOperation _mapping;

	public MergeSyncBatchOperation(ISharedContext sharedContext, IMapSyncEntityOperation mapping)
	{
		_sharedContext = sharedContext;
		_mapping = mapping;
	}

	public void Run(SyncBatch batch, Guid localRegistrationId, long revision, Dictionary<string, SyncEntityState> sets, Dictionary<long, Guid> sources, CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		SyncBatchMergeState state = new() { Batch = batch, LocalRegistrationId = localRegistrationId, Revision = revision, Sets = sets, Sources = sources };
		IndexCurrencyRates(state);
		foreach ((SyncObject item, bool added, bool deleted) in EnumerateChanges(state))
		{
			cancellationToken.ThrowIfCancellationRequested();
			MergeObject(state, item, added, deleted);
		}
	}

	private IEnumerable<(SyncObject Item, bool Added, bool Deleted)> EnumerateChanges(SyncBatchMergeState state) =>
		state.Batch.Added.Select(item => (item, true, false))
			.Concat(state.Batch.Updated.Select(item => (item, false, false)))
			.Concat(state.Batch.Deleted.Select(item => (item, false, true)));

	private void IndexCurrencyRates(SyncBatchMergeState state)
	{
		foreach (SyncMergeRow row in state.Sets[nameof(CurrencyRate)].Rows.Values)
		{
			CurrencyRate rate = (CurrencyRate)row.Entity;
			if (!state.Rates.TryAdd((rate.CurrencyId, rate.Date), row))
			{
				throw new InvalidSyncBatchException("Master contains duplicate rates for a currency and date.");
			}
		}
	}

	private void MergeObject(SyncBatchMergeState state, SyncObject item, bool added, bool deleted)
	{
		if (item is null)
		{
			throw new InvalidSyncBatchException("A batch object cannot be null.");
		}
		added = added || (deleted && item.EditRevision is null);
		SyncEntityState set = ValidateIdentity(state, item, added);
		(bool content, bool order) = ValidateFlags(item, set, added, deleted);
		BaseEntity candidate = _mapping.Read(set, item, content, set.IsOrdered && (added || deleted || order));
		SyncMergeRow? row = FindCurrentRow(state, set, candidate);
		ValidateExistence(item, candidate, row, added);
		if (IgnoreProtectedRoot(state, item, set)) { return; }
		if (row is null)
		{
			row = AddCandidate(state, set, candidate);
			row.Tracking.DeleteRevision = deleted ? state.Revision : null;
		}
		else if (content && IncomingContentWins(state, item, row, added)) { ReplaceContent(state, row, candidate, deleted, order); }
		if (order && row.IsLive) { ((IOrderedEntity)row.Entity).Order = ((IOrderedEntity)candidate).Order; }
	}

	private SyncEntityState ValidateIdentity(SyncBatchMergeState state, SyncObject item, bool added)
	{
		if (item is null || string.IsNullOrEmpty(item.Type) || !state.Sets.TryGetValue(item.Type, out SyncEntityState? set) ||
			item.Id == Guid.Empty || !state.Identities.Add((item.Type, item.Id)) || item.EditRevision < 0 || item.EditRevision >= state.Revision ||
			(added != (item.EditRevision is null)) ||
			(item.ModificationType & ~(ModificationType.Content | ModificationType.Order)) != ModificationType.None)
		{
			throw new InvalidSyncBatchException("Invalid entity type, identity, revision, flags or duplicate batch object.");
		}
		return set;
	}

	private static (bool Content, bool Order) ValidateFlags(SyncObject item, SyncEntityState set, bool added, bool deleted)
	{
		bool content = added || item.ModificationType.HasFlag(ModificationType.Content);
		bool order = item.ModificationType.HasFlag(ModificationType.Order);
		if ((!added && !content && !order) || (deleted && !content) || (order && !set.IsOrdered))
		{
			throw new InvalidSyncBatchException($"{item.Type}/{item.Id}: flags do not describe a valid change.");
		}
		return (content, order);
	}

	private SyncMergeRow? FindCurrentRow(SyncBatchMergeState state, SyncEntityState set, BaseEntity candidate)
	{
		set.Rows.TryGetValue(candidate.Id, out SyncMergeRow? row);
		if (candidate is not CurrencyRate rate) { return row; }
		ValidateCurrencyRateIdentity(state, rate, row);
		state.Rates.TryGetValue((rate.CurrencyId, rate.Date), out row);
		if (row is not null) { candidate.Id = row.Entity.Id; }
		return row;
	}

	private void ValidateCurrencyRateIdentity(SyncBatchMergeState state, CurrencyRate rate, SyncMergeRow? row)
	{
		if (row?.Entity is CurrencyRate original && (original.CurrencyId != rate.CurrencyId || original.Date != rate.Date))
		{
			throw new InvalidSyncBatchException($"Currency rate {rate.Id} cannot change its currency/date identity.");
		}
		if (!state.RateKeys.Add((rate.CurrencyId, rate.Date)))
		{
			throw new InvalidSyncBatchException("A batch contains multiple changes for the same currency and date.");
		}
	}

	private static void ValidateExistence(SyncObject item, BaseEntity candidate, SyncMergeRow? row, bool added)
	{
		if ((!added && row is null) || (added && row is not null && candidate is not (Currency or CurrencyRate)))
		{
			throw new InvalidSyncBatchException($"{item.Type}/{item.Id}: creation collides with an existing identity or the edited entity is missing.");
		}
	}

	private bool IgnoreProtectedRoot(SyncBatchMergeState state, SyncObject item, SyncEntityState set)
	{
		if (set.RootId != item.Id) { return false; }
		_sharedContext.LogService.Warn($"Batch {state.Batch.SyncKey}: ignored a change to protected root {item.Type}/{item.Id}.");
		return true;
	}

	private SyncMergeRow AddCandidate(SyncBatchMergeState state, SyncEntityState set, BaseEntity candidate)
	{
		SyncMergeRow row = new() { Entity = candidate, IncomingContent = true };
		set.Rows.Add(candidate.Id, row);
		if (candidate is CurrencyRate rate) { state.Rates.Add((rate.CurrencyId, rate.Date), row); }
		return row;
	}

	private bool IncomingContentWins(SyncBatchMergeState state, SyncObject item, SyncMergeRow row, bool added) =>
		(!added && item.EditRevision == row.Tracking.EditRevision) || state.Batch.Priority == ConflictPriority.Local ||
		(row.Tracking.EditRevision is long previous && state.Sources.GetValueOrDefault(previous) == state.LocalRegistrationId);

	private void ReplaceContent(SyncBatchMergeState state, SyncMergeRow row, BaseEntity candidate, bool deleted, bool order)
	{
		int? masterOrder = (row.Entity as IOrderedEntity)?.Order;
		long? masterDeletion = row.Tracking.DeleteRevision;
		row.Entity = candidate;
		row.Tracking.EditRevision = row.OriginalEditRevision;
		row.Tracking.DeleteRevision = deleted ? masterDeletion ?? state.Revision : null;
		if (candidate is IOrderedEntity ordered && !order) { ordered.Order = masterOrder!.Value; }
		row.IncomingContent = true;
	}
}
