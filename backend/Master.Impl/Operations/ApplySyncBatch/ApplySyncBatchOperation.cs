using Business.Core.Entities;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using Business.Models.Entities.Reporting;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using Exchange.Models.Enums;
using Exchange.Models.Synchronizations;
using Shared.Contracts;
using StoredModificationType = Business.Models.Enums.ModificationType;

namespace Master.Impl.Operations.ApplySyncBatch;

/// <summary>
/// Reconciles one immutable Local batch against the current Master state.
/// Uses explicit exchange mappings and accepted revision provenance.
/// Resolves whole-entity content independently from catalog ordering.
/// Repairs dependencies, hierarchy and naming before writing accepted rows.
/// Transaction and template entries remain indivisible content aggregates.
/// Content changes receive the caller's allocated revision; ordering alone does not.
/// Saves through the Master unit of work inside the caller-owned transaction.
/// Receipt creation, deduplication, publication and transaction completion are excluded.
/// </summary>
internal sealed class ApplySyncBatchOperation : IApplySyncBatchOperation
{
	private readonly IMasterAppUnitOfWork _unitOfWork;
	private readonly ISharedContext _sharedContext;
	private readonly ICumulativeAmountCommand _cumulativeAmountCommand;

	public ApplySyncBatchOperation(ISharedContext sharedContext, IMasterAppUnitOfWork unitOfWork,
		ICumulativeAmountCommand cumulativeAmountCommand)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_cumulativeAmountCommand = cumulativeAmountCommand;
	}

	public async Task Run(SyncBatch syncBatch, Guid localRegistrationId, long revision,
		CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(syncBatch);
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrWhiteSpace(syncBatch.SyncKey) || syncBatch.CreatedAt.Kind != DateTimeKind.Utc ||
			syncBatch.Priority is not (ConflictPriority.Local or ConflictPriority.Master) || revision <= 0 ||
			localRegistrationId == Guid.Empty)
		{
			throw new InvalidSyncBatchException("A batch requires its key, UTC time, conflict priority, registered Local and positive revision.");
		}
		if (await _unitOfWork.LocalRegistrationRepo.GetById(localRegistrationId, cancellationToken) is null)
		{
			throw new InvalidSyncBatchException("The submitting Local is not registered on this Master.");
		}
		ICollection<SystemConfig> configurations = await _unitOfWork.SystemConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1 || configurations.Single().CurrentRevision >= revision)
		{
			throw new InvalidSyncBatchException("The Master configuration or allocated revision is invalid.");
		}
		ICollection<LocalSyncBatch> receipts = await _unitOfWork.LocalSyncBatchRepo.GetAll(cancellationToken);
		if (receipts.Any(receipt => receipt.Revision >= revision ||
			(receipt.LocalRegistrationId == localRegistrationId && receipt.SyncKey == syncBatch.SyncKey)) ||
			receipts.GroupBy(receipt => receipt.Revision).Any(group => group.Count() != 1))
		{
			throw new InvalidSyncBatchException("The caller must deduplicate the batch and allocate a new Master revision before applying it.");
		}
		Dictionary<long, Guid> sources = receipts.ToDictionary(receipt => receipt.Revision, receipt => receipt.LocalRegistrationId);
		Dictionary<string, SyncEntitySet> sets = CreateSets();
		foreach (SyncEntitySet set in sets.Values) { await set.Load(cancellationToken); }
		ICollection<TransactionEntry> transactionEntries = await _unitOfWork.TransactionEntryRepo.GetAll(cancellationToken);
		ICollection<TemplateEntry> templateEntries = await _unitOfWork.TemplateEntryRepo.GetAll(cancellationToken);
		ILookup<Guid, TransactionEntry> transactionsByParent = transactionEntries.ToLookup(entry => entry.TransactionId);
		ILookup<Guid, TemplateEntry> templatesByParent = templateEntries.ToLookup(entry => entry.TemplateId);
		foreach (SyncMergeRow row in sets["transaction"].Rows.Values)
		{
			((Transaction)row.Entity).Entries = transactionsByParent[row.Entity.Id].OrderBy(entry => entry.Position).ToList();
			sets["transaction"].RememberAggregate(row);
		}
		foreach (SyncMergeRow row in sets["template"].Rows.Values)
		{
			((Template)row.Entity).Entries = templatesByParent[row.Entity.Id].OrderBy(entry => entry.Position).ToList();
			sets["template"].RememberAggregate(row);
		}
		HashSet<(string Type, Guid Id)> identities = new();
		HashSet<(Guid CurrencyId, DateOnly Date)> rateKeys = new();
		Dictionary<(Guid CurrencyId, DateOnly Date), SyncMergeRow> rates = new();
		foreach (SyncMergeRow existing in sets["currencyRate"].Rows.Values)
		{
			CurrencyRate rate = (CurrencyRate)existing.Entity;
			if (!rates.TryAdd((rate.CurrencyId, rate.Date), existing))
			{
				throw new InvalidSyncBatchException("Master contains duplicate rates for a currency and date.");
			}
		}
		foreach ((SyncObject item, bool added, bool deleted) in syncBatch.Added.Select(item => (item, true, false))
			.Concat(syncBatch.Updated.Select(item => (item, false, false)))
			.Concat(syncBatch.Deleted.Select(item => (item, false, true))))
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (item is null || string.IsNullOrEmpty(item.Type) || !sets.TryGetValue(item.Type, out SyncEntitySet? set) ||
				item.Id == Guid.Empty || !identities.Add((item.Type, item.Id)) || item.EditRevision < 0 || item.EditRevision >= revision ||
				(added != (item.EditRevision is null)) ||
				(item.ModificationType & ~(ModificationType.Content | ModificationType.Order)) != ModificationType.None)
			{
				throw new InvalidSyncBatchException("Invalid entity type, identity, revision, flags or duplicate batch object.");
			}
			bool content = added || item.ModificationType.HasFlag(ModificationType.Content);
			bool order = item.ModificationType.HasFlag(ModificationType.Order);
			if ((!added && !content && !order) || (deleted && !content) || (order && !set.IsOrdered))
			{
				throw new InvalidSyncBatchException($"{item.Type}/{item.Id}: flags do not describe a valid change.");
			}
			BaseEntity candidate = set.Read(item, content, set.IsOrdered && (added || deleted || order));
			bool exists = set.Rows.TryGetValue(item.Id, out SyncMergeRow? row);
			if (candidate is CurrencyRate rate)
			{
				if (exists && row!.Entity is CurrencyRate original && (original.CurrencyId != rate.CurrencyId || original.Date != rate.Date))
				{
					throw new InvalidSyncBatchException($"Currency rate {item.Id} cannot change its currency/date identity.");
				}
				if (!rateKeys.Add((rate.CurrencyId, rate.Date)))
				{
					throw new InvalidSyncBatchException("A batch contains multiple changes for the same currency and date.");
				}
				exists = rates.TryGetValue((rate.CurrencyId, rate.Date), out row);
				if (exists) { candidate.Id = row!.Entity.Id; }
			}
			if ((!added && !exists) || (added && exists && candidate is not (Currency or CurrencyRate)))
			{
				throw new InvalidSyncBatchException($"{item.Type}/{item.Id}: creation collides with an existing identity or the edited entity is missing.");
			}
			if (set.RootId == item.Id)
			{
				_sharedContext.LogService.Warn($"Batch {syncBatch.SyncKey}: ignored a change to protected root {item.Type}/{item.Id}.");
				continue;
			}
			if (!exists)
			{
				row = new SyncMergeRow { Entity = candidate, IncomingContent = true };
				set.Rows.Add(candidate.Id, row);
				if (candidate is CurrencyRate newRate) { rates.Add((newRate.CurrencyId, newRate.Date), row); }
			}
			else if (row is not null && content && ((!added && item.EditRevision == row.Tracking.EditRevision) || syncBatch.Priority == ConflictPriority.Local ||
				(row.Tracking.EditRevision is long previous && sources.GetValueOrDefault(previous) == localRegistrationId)))
			{
				int? masterOrder = (row.Entity as IOrderedEntity)?.Order;
				long? masterDeletion = row.Tracking.DeleteRevision;
				row.Entity = candidate;
				row.Tracking.EditRevision = row.OriginalEditRevision;
				row.Tracking.DeleteRevision = deleted ? masterDeletion ?? revision : null;
				if (candidate is IOrderedEntity ordered && !order) { ordered.Order = masterOrder!.Value; }
				row.IncomingContent = true;
			}
			if (order && row!.IsLive) { ((IOrderedEntity)row.Entity).Order = ((IOrderedEntity)candidate).Order; }
		}
		SyncBatchValidation validation = new(sets, configurations.Single(), _sharedContext.LogService, syncBatch.Priority);
		validation.Run(cancellationToken);
		List<(SyncEntitySet Set, SyncMergeRow Row)> changed = new();
		foreach (SyncEntitySet set in sets.Values)
		{
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				bool contentChanged = set.ContentChanged(row) || row.IsLive != (row.OriginalDeleteRevision is null);
				row.Tracking.EditRevision = contentChanged ? revision : row.OriginalEditRevision;
				row.Tracking.DeleteRevision = row.IsLive ? null : contentChanged ? revision : row.OriginalDeleteRevision;
				row.Tracking.ModificationType = StoredModificationType.None;
				if (contentChanged || (row.Entity as IOrderedEntity)?.Order != row.OriginalOrder)
				{
					changed.Add((set, row));
				}
			}
		}
		Dictionary<Guid, Guid> transactionOwners = transactionEntries.ToDictionary(entry => entry.Id, entry => entry.TransactionId);
		Dictionary<Guid, Guid> templateOwners = templateEntries.ToDictionary(entry => entry.Id, entry => entry.TemplateId);
		foreach ((SyncEntitySet _, SyncMergeRow row) in changed)
		{
			bool changedOwner = row.Entity switch
			{
				Transaction transaction => transaction.Entries.Any(entry => transactionOwners.TryGetValue(entry.Id, out Guid owner) && owner != transaction.Id),
				Template template => template.Entries.Any(entry => templateOwners.TryGetValue(entry.Id, out Guid owner) && owner != template.Id),
				_ => false
			};
			if (changedOwner) { throw new InvalidSyncBatchException("An entry identity cannot move to another aggregate."); }
		}
		foreach ((SyncEntitySet set, SyncMergeRow row) in changed) { set.Persist(row); }
		await _unitOfWork.SaveChanges(cancellationToken);
		HashSet<Guid> affectedAccounts = new();
		foreach ((SyncEntitySet set, SyncMergeRow row) in changed)
		{
			if (row.Entity is Transaction transaction)
			{
				List<TransactionEntry> old = transactionsByParent[transaction.Id].ToList();
				affectedAccounts.UnionWith(old.Select(entry => entry.AccountId));
				affectedAccounts.UnionWith(transaction.Entries.Select(entry => entry.AccountId));
				await _unitOfWork.TransactionEntryRepo.RemoveRange(old.Where(entry => transaction.Entries.All(current => current.Id != entry.Id)), cancellationToken);
				HashSet<Guid> oldIds = old.Select(entry => entry.Id).ToHashSet();
				foreach (TransactionEntry entry in transaction.Entries)
				{
					if (oldIds.Contains(entry.Id)) { _unitOfWork.TransactionEntryRepo.Update(entry); }
					else { _unitOfWork.TransactionEntryRepo.Add(entry); }
				}
			}
			else if (row.Entity is Template template && set.ContentChanged(row))
			{
				List<TemplateEntry> old = templatesByParent[template.Id].ToList();
				await _unitOfWork.TemplateEntryRepo.RemoveRange(old.Where(entry => template.Entries.All(current => current.Id != entry.Id)), cancellationToken);
				HashSet<Guid> oldIds = old.Select(entry => entry.Id).ToHashSet();
				foreach (TemplateEntry entry in template.Entries)
				{
					if (oldIds.Contains(entry.Id)) { _unitOfWork.TemplateEntryRepo.Update(entry); }
					else { _unitOfWork.TemplateEntryRepo.Add(entry); }
				}
			}
		}
		await _unitOfWork.SaveChanges(cancellationToken);
		foreach (Guid accountId in affectedAccounts)
		{
			await _cumulativeAmountCommand.Recalculate(accountId, cancellationToken);
		}
	}

	private Dictionary<string, SyncEntitySet> CreateSets()
	{
		string[] group = ["Name", "Description", "IsFavorite", "ParentId"];
		string[] element = ["Name", "Description", "IsFavorite", "GroupId"];
		SyncEntitySet[] sets =
		[
			SyncEntitySet.Create("currency", _unitOfWork.CurrencyRepo, ["Code", "Symbol", "EnglishName", "Name", "IsFavorite"]),
			SyncEntitySet.Create("currencyRate", _unitOfWork.CurrencyRateRepo, ["CurrencyId", "Date", "Rate", "Description"]),
			SyncEntitySet.Create("accountGroup", _unitOfWork.AccountGroupRepo, group, RootsIds.AccountGroupId, "accountGroup", "parentId"),
			SyncEntitySet.Create("categoryGroup", _unitOfWork.CategoryGroupRepo, group, RootsIds.CategoryGroupId, "categoryGroup", "parentId"),
			SyncEntitySet.Create("correspondentGroup", _unitOfWork.CorrespondentGroupRepo, group, RootsIds.CorrespondentGroupId, "correspondentGroup", "parentId"),
			SyncEntitySet.Create("projectGroup", _unitOfWork.ProjectGroupRepo, group, RootsIds.ProjectGroupId, "projectGroup", "parentId"),
			SyncEntitySet.Create("templateGroup", _unitOfWork.TemplateGroupRepo, group, RootsIds.TemplateGroupId, "templateGroup", "parentId"),
			SyncEntitySet.Create("reportGroup", _unitOfWork.ReportGroupRepo, group, RootsIds.ReportGroupId, "reportGroup", "parentId"),
			SyncEntitySet.Create("category", _unitOfWork.CategoryRepo, element, parentType: "categoryGroup", parentField: "groupId"),
			SyncEntitySet.Create("correspondent", _unitOfWork.CorrespondentRepo, element, parentType: "correspondentGroup", parentField: "groupId"),
			SyncEntitySet.Create("project", _unitOfWork.ProjectRepo, element, parentType: "projectGroup", parentField: "groupId"),
			SyncEntitySet.Create("account", _unitOfWork.AccountRepo, [.. element, "CurrencyId", "CategoryId", "CorrespondentId", "ProjectId"], parentType: "accountGroup", parentField: "groupId"),
			SyncEntitySet.Create("template", _unitOfWork.TemplateRepo, [.. element, "Entries"], parentType: "templateGroup", parentField: "groupId"),
			SyncEntitySet.Create("report", _unitOfWork.ReportRepo, [.. element, "Json"], parentType: "reportGroup", parentField: "groupId"),
			SyncEntitySet.Create("transaction", _unitOfWork.TransactionRepo, ["DateTime", "State", "Description", "Entries"])
		];
		return sets.ToDictionary(set => set.Type, StringComparer.Ordinal);
	}
}
