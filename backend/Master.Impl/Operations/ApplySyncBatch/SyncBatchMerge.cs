using Business.Core.Entities;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using Exchange.Models.Enums;
using Exchange.Models.Synchronizations;
using Shared.Contracts;

namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Reconciles incoming objects with the detached state of one Master batch.
/// One instance owns duplicate-detection indexes for one application only.
/// Currency rates match by currency and date while preserving Master identity.
/// Whole-entity content conflicts use revisions, priority and Local provenance.
/// Ordering is applied independently to live rows after content resolution.
/// Protected roots are logged and preserved without incoming changes.
/// Resulting graph validation and revision stamping remain caller responsibilities.
/// This helper does not access repositories, save changes or own transactions.
/// </summary>
internal sealed class SyncBatchMerge
{
	private readonly SyncBatch _batch;
	private readonly Guid _localRegistrationId;
	private readonly long _revision;
	private readonly Dictionary<string, SyncEntitySet> _sets;
	private readonly Dictionary<long, Guid> _sources;
	private readonly ILogService _log;
	private readonly HashSet<(string Type, Guid Id)> _identities = new();
	private readonly HashSet<(Guid CurrencyId, DateOnly Date)> _rateKeys = new();
	private readonly Dictionary<(Guid CurrencyId, DateOnly Date), SyncMergeRow> _rates = new();

	public SyncBatchMerge(SyncBatch batch, Guid localRegistrationId, long revision,
		Dictionary<string, SyncEntitySet> sets, Dictionary<long, Guid> sources, ILogService log)
	{
		_batch = batch;
		_localRegistrationId = localRegistrationId;
		_revision = revision;
		_sets = sets;
		_sources = sources;
		_log = log;
	}

	public void Run(CancellationToken cancellationToken)
	{
		IndexCurrencyRates();
		foreach ((SyncObject item, bool added, bool deleted) in EnumerateChanges())
		{
			cancellationToken.ThrowIfCancellationRequested();
			MergeObject(item, added, deleted);
		}
	}

	private IEnumerable<(SyncObject Item, bool Added, bool Deleted)> EnumerateChanges() =>
		_batch.Added.Select(item => (item, true, false))
			.Concat(_batch.Updated.Select(item => (item, false, false)))
			.Concat(_batch.Deleted.Select(item => (item, false, true)));

	private void IndexCurrencyRates()
	{
		foreach (SyncMergeRow row in _sets["currencyRate"].Rows.Values)
		{
			CurrencyRate rate = (CurrencyRate)row.Entity;
			if (!_rates.TryAdd((rate.CurrencyId, rate.Date), row))
			{
				throw new InvalidSyncBatchException("Master contains duplicate rates for a currency and date.");
			}
		}
	}

	private void MergeObject(SyncObject item, bool added, bool deleted)
	{
		SyncEntitySet set = ValidateIdentity(item, added);
		(bool content, bool order) = ValidateFlags(item, set, added, deleted);
		BaseEntity candidate = set.Read(item, content, set.IsOrdered && (added || deleted || order));
		SyncMergeRow? row = FindCurrentRow(set, candidate);
		ValidateExistence(item, candidate, row, added);
		if (IgnoreProtectedRoot(item, set)) { return; }
		if (row is null) { row = AddCandidate(set, candidate); }
		else if (content && IncomingContentWins(item, row, added)) { ReplaceContent(row, candidate, deleted, order); }
		if (order && row.IsLive) { ((IOrderedEntity)row.Entity).Order = ((IOrderedEntity)candidate).Order; }
	}

	private SyncEntitySet ValidateIdentity(SyncObject item, bool added)
	{
		if (item is null || string.IsNullOrEmpty(item.Type) || !_sets.TryGetValue(item.Type, out SyncEntitySet? set) ||
			item.Id == Guid.Empty || !_identities.Add((item.Type, item.Id)) || item.EditRevision < 0 || item.EditRevision >= _revision ||
			(added != (item.EditRevision is null)) ||
			(item.ModificationType & ~(ModificationType.Content | ModificationType.Order)) != ModificationType.None)
		{
			throw new InvalidSyncBatchException("Invalid entity type, identity, revision, flags or duplicate batch object.");
		}
		return set;
	}

	private static (bool Content, bool Order) ValidateFlags(SyncObject item, SyncEntitySet set, bool added, bool deleted)
	{
		bool content = added || item.ModificationType.HasFlag(ModificationType.Content);
		bool order = item.ModificationType.HasFlag(ModificationType.Order);
		if ((!added && !content && !order) || (deleted && !content) || (order && !set.IsOrdered))
		{
			throw new InvalidSyncBatchException($"{item.Type}/{item.Id}: flags do not describe a valid change.");
		}
		return (content, order);
	}

	private SyncMergeRow? FindCurrentRow(SyncEntitySet set, BaseEntity candidate)
	{
		set.Rows.TryGetValue(candidate.Id, out SyncMergeRow? row);
		if (candidate is not CurrencyRate rate) { return row; }
		ValidateCurrencyRateIdentity(rate, row);
		_rates.TryGetValue((rate.CurrencyId, rate.Date), out row);
		if (row is not null) { candidate.Id = row.Entity.Id; }
		return row;
	}

	private void ValidateCurrencyRateIdentity(CurrencyRate rate, SyncMergeRow? row)
	{
		if (row?.Entity is CurrencyRate original && (original.CurrencyId != rate.CurrencyId || original.Date != rate.Date))
		{
			throw new InvalidSyncBatchException($"Currency rate {rate.Id} cannot change its currency/date identity.");
		}
		if (!_rateKeys.Add((rate.CurrencyId, rate.Date)))
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

	private bool IgnoreProtectedRoot(SyncObject item, SyncEntitySet set)
	{
		if (set.RootId != item.Id) { return false; }
		_log.Warn($"Batch {_batch.SyncKey}: ignored a change to protected root {item.Type}/{item.Id}.");
		return true;
	}

	private SyncMergeRow AddCandidate(SyncEntitySet set, BaseEntity candidate)
	{
		SyncMergeRow row = new() { Entity = candidate, IncomingContent = true };
		set.Rows.Add(candidate.Id, row);
		if (candidate is CurrencyRate rate) { _rates.Add((rate.CurrencyId, rate.Date), row); }
		return row;
	}

	private bool IncomingContentWins(SyncObject item, SyncMergeRow row, bool added) =>
		(!added && item.EditRevision == row.Tracking.EditRevision) || _batch.Priority == ConflictPriority.Local ||
		(row.Tracking.EditRevision is long previous && _sources.GetValueOrDefault(previous) == _localRegistrationId);

	private void ReplaceContent(SyncMergeRow row, BaseEntity candidate, bool deleted, bool order)
	{
		int? masterOrder = (row.Entity as IOrderedEntity)?.Order;
		long? masterDeletion = row.Tracking.DeleteRevision;
		row.Entity = candidate;
		row.Tracking.EditRevision = row.OriginalEditRevision;
		row.Tracking.DeleteRevision = deleted ? masterDeletion ?? _revision : null;
		if (candidate is IOrderedEntity ordered && !order) { ordered.Order = masterOrder!.Value; }
		row.IncomingContent = true;
	}
}
