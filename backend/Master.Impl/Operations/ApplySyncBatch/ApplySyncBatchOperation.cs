using Business.Core.Entities;
using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using Business.Models.Entities.Synchronizations;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Core.Behaviors;
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
/// Resolves the Master unit from the scoped factory using the supplied dataset key.
/// The cached unit shares the transaction already opened by the caller.
/// Receipt creation, deduplication, publication and transaction completion are excluded.
/// </summary>
internal sealed class ApplySyncBatchOperation : IApplySyncBatchOperation
{
	private readonly ISharedContext _sharedContext;
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;

	public ApplySyncBatchOperation(ISharedContext sharedContext, IUnitOfWorkFactory unitOfWorkFactory)
	{
		_sharedContext = sharedContext;
		_unitOfWorkFactory = unitOfWorkFactory;
	}

	public async Task Run(
		string masterDatasetKey,
		SyncBatch syncBatch,
		Guid localRegistrationId,
		long revision,
		CancellationToken cancellationToken = default)
	{
		ValidateBatch(syncBatch, localRegistrationId, revision, cancellationToken);
		IMasterUnitOfWork unitOfWork = _unitOfWorkFactory.CreateMaster(masterDatasetKey);
		SystemConfig configuration = await LoadConfiguration(
			unitOfWork,
			localRegistrationId,
			revision,
			cancellationToken);
		Dictionary<long, Guid> sources = await LoadRevisionSources(
			unitOfWork,
			syncBatch,
			localRegistrationId,
			revision,
			cancellationToken);
		Dictionary<string, SyncEntitySet> sets = await LoadSets(unitOfWork, cancellationToken);
		(ICollection<TransactionEntry> transactions, ICollection<TemplateEntry> templates) = await LoadEntries(unitOfWork, cancellationToken);
		AttachEntries(sets, transactions, templates);
		MergeChanges(syncBatch, localRegistrationId, revision, sets, sources, cancellationToken);
		ValidateResult(sets, configuration, syncBatch.Priority, cancellationToken);
		List<(SyncEntitySet Set, SyncMergeRow Row)> changed = PrepareChanges(sets, revision);
		ValidateEntryOwners(changed, transactions, templates);
		await PersistChanges(
			unitOfWork,
			changed,
			transactions,
			templates,
			cancellationToken);
	}

	private static void ValidateBatch(SyncBatch batch, Guid localRegistrationId, long revision, CancellationToken cancellationToken)
	{
		ArgumentNullException.ThrowIfNull(batch);
		cancellationToken.ThrowIfCancellationRequested();
		if (string.IsNullOrWhiteSpace(batch.SyncKey) || batch.CreatedAt.Kind != DateTimeKind.Utc ||
			batch.Priority is not (ConflictPriority.Local or ConflictPriority.Master) || revision <= 0 || localRegistrationId == Guid.Empty)
		{
			throw new InvalidSyncBatchException("A batch requires its key, UTC time, conflict priority, registered Local and positive revision.");
		}
	}

	private static async Task<SystemConfig> LoadConfiguration(
		IMasterUnitOfWork unitOfWork,
		Guid localRegistrationId,
		long revision,
		CancellationToken cancellationToken)
	{
		if (await unitOfWork.LocalRegistrationRepo.GetById(localRegistrationId, cancellationToken) is null)
		{
			throw new InvalidSyncBatchException("The submitting Local is not registered on this Master.");
		}
		ICollection<SystemConfig> configurations = await unitOfWork.SystemConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1 || configurations.Single().CurrentRevision >= revision)
		{
			throw new InvalidSyncBatchException("The Master configuration or allocated revision is invalid.");
		}
		return configurations.Single();
	}

	private static async Task<Dictionary<long, Guid>> LoadRevisionSources(
		IMasterUnitOfWork unitOfWork,
		SyncBatch batch,
		Guid localRegistrationId,
		long revision,
		CancellationToken cancellationToken)
	{
		ICollection<LocalSyncBatch> receipts = await unitOfWork.LocalSyncBatchRepo.GetAll(cancellationToken);
		if (receipts.Any(receipt => receipt.Revision >= revision ||
			(receipt.LocalRegistrationId == localRegistrationId && receipt.SyncKey == batch.SyncKey)) ||
			receipts.GroupBy(receipt => receipt.Revision).Any(group => group.Count() != 1))
		{
			throw new InvalidSyncBatchException("The caller must deduplicate the batch and allocate a new Master revision before applying it.");
		}
		return receipts.ToDictionary(receipt => receipt.Revision, receipt => receipt.LocalRegistrationId);
	}

	private static async Task<Dictionary<string, SyncEntitySet>> LoadSets(
		IMasterUnitOfWork unitOfWork,
		CancellationToken cancellationToken)
	{
		Dictionary<string, SyncEntitySet> sets = CreateSets(unitOfWork);
		foreach (SyncEntitySet set in sets.Values)
		{
			await set.Load(cancellationToken);
		}
		return sets;
	}

	private static async Task<(ICollection<TransactionEntry> Transactions, ICollection<TemplateEntry> Templates)> LoadEntries(
		IMasterUnitOfWork unitOfWork,
		CancellationToken cancellationToken)
	{
		ICollection<TransactionEntry> transactions = await unitOfWork.TransactionEntryRepo.GetAll(cancellationToken);
		ICollection<TemplateEntry> templates = await unitOfWork.TemplateEntryRepo.GetAll(cancellationToken);
		return (transactions, templates);
	}

	private static void AttachEntries(Dictionary<string, SyncEntitySet> sets,
		ICollection<TransactionEntry> transactions, ICollection<TemplateEntry> templates)
	{
		AttachTransactionEntries(sets["transaction"], transactions.ToLookup(entry => entry.TransactionId));
		AttachTemplateEntries(sets["template"], templates.ToLookup(entry => entry.TemplateId));
	}

	private static void AttachTransactionEntries(SyncEntitySet set, ILookup<Guid, TransactionEntry> entries)
	{
		foreach (SyncMergeRow row in set.Rows.Values)
		{
			((Transaction)row.Entity).Entries = entries[row.Entity.Id].OrderBy(entry => entry.Position).ToList();
			set.RememberAggregate(row);
		}
	}

	private static void AttachTemplateEntries(SyncEntitySet set, ILookup<Guid, TemplateEntry> entries)
	{
		foreach (SyncMergeRow row in set.Rows.Values)
		{
			((Template)row.Entity).Entries = entries[row.Entity.Id].OrderBy(entry => entry.Position).ToList();
			set.RememberAggregate(row);
		}
	}

	private void MergeChanges(SyncBatch batch, Guid localRegistrationId, long revision,
		Dictionary<string, SyncEntitySet> sets, Dictionary<long, Guid> sources, CancellationToken cancellationToken)
	{
		SyncBatchMerge merge = new(batch, localRegistrationId, revision, sets, sources, _sharedContext.LogService);
		merge.Run(cancellationToken);
	}

	private void ValidateResult(Dictionary<string, SyncEntitySet> sets, SystemConfig configuration,
		ConflictPriority priority, CancellationToken cancellationToken)
	{
		SyncBatchValidation validation = new(sets, configuration, _sharedContext.LogService, priority);
		validation.Run(cancellationToken);
	}

	private static List<(SyncEntitySet Set, SyncMergeRow Row)> PrepareChanges(Dictionary<string, SyncEntitySet> sets, long revision)
	{
		List<(SyncEntitySet Set, SyncMergeRow Row)> changed = new();
		foreach (SyncEntitySet set in sets.Values)
		{
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				if (StampChanges(set, row, revision)) { changed.Add((set, row)); }
			}
		}
		return changed;
	}

	private static bool StampChanges(SyncEntitySet set, SyncMergeRow row, long revision)
	{
		bool contentChanged = set.ContentChanged(row) || row.IsLive != row.OriginalDeleteRevision is null;
		row.Tracking.EditRevision = contentChanged ? revision : row.OriginalEditRevision;
		row.Tracking.DeleteRevision = row.IsLive ? null : contentChanged ? revision : row.OriginalDeleteRevision;
		row.Tracking.ModificationType = StoredModificationType.None;
		return contentChanged || (row.Entity as IOrderedEntity)?.Order != row.OriginalOrder;
	}

	private static void ValidateEntryOwners(List<(SyncEntitySet Set, SyncMergeRow Row)> changed,
		ICollection<TransactionEntry> transactions, ICollection<TemplateEntry> templates)
	{
		Dictionary<Guid, Guid> transactionOwners = transactions.ToDictionary(entry => entry.Id, entry => entry.TransactionId);
		Dictionary<Guid, Guid> templateOwners = templates.ToDictionary(entry => entry.Id, entry => entry.TemplateId);
		foreach ((SyncEntitySet _, SyncMergeRow row) in changed)
		{
			if (HasChangedEntryOwner(row.Entity, transactionOwners, templateOwners))
			{
				throw new InvalidSyncBatchException("An entry identity cannot move to another aggregate.");
			}
		}
	}

	private static bool HasChangedEntryOwner(BaseEntity entity, Dictionary<Guid, Guid> transactionOwners,
		Dictionary<Guid, Guid> templateOwners) => entity switch
	{
		Transaction transaction => transaction.Entries.Any(entry => transactionOwners.TryGetValue(entry.Id, out Guid owner) && owner != transaction.Id),
		Template template => template.Entries.Any(entry => templateOwners.TryGetValue(entry.Id, out Guid owner) && owner != template.Id),
		_ => false
	};

	private static async Task PersistChanges(
		IMasterUnitOfWork unitOfWork,
		List<(SyncEntitySet Set, SyncMergeRow Row)> changed,
		ICollection<TransactionEntry> transactions,
		ICollection<TemplateEntry> templates,
		CancellationToken cancellationToken)
	{
		await SaveEntities(
			unitOfWork,
			changed,
			cancellationToken);
		await SaveEntries(
			unitOfWork,
			changed,
			transactions,
			templates,
			cancellationToken);
	}

	private static async Task SaveEntities(
		IMasterUnitOfWork unitOfWork,
		List<(SyncEntitySet Set, SyncMergeRow Row)> changed,
		CancellationToken cancellationToken)
	{
		foreach ((SyncEntitySet set, SyncMergeRow row) in changed)
		{
			set.Persist(row);
		}
		await unitOfWork.SaveChanges(cancellationToken);
	}

	private static async Task SaveEntries(
		IMasterUnitOfWork unitOfWork,
		List<(SyncEntitySet Set, SyncMergeRow Row)> changed,
		ICollection<TransactionEntry> transactions,
		ICollection<TemplateEntry> templates,
		CancellationToken cancellationToken)
	{
		ILookup<Guid, TransactionEntry> transactionsByParent = transactions.ToLookup(entry => entry.TransactionId);
		ILookup<Guid, TemplateEntry> templatesByParent = templates.ToLookup(entry => entry.TemplateId);
		foreach ((SyncEntitySet set, SyncMergeRow row) in changed)
		{
			await SaveAggregateEntries(
				unitOfWork,
				set,
				row,
				transactionsByParent,
				templatesByParent,
				cancellationToken);
		}
		await unitOfWork.SaveChanges(cancellationToken);
	}

	private static async Task SaveAggregateEntries(
		IMasterUnitOfWork unitOfWork,
		SyncEntitySet set,
		SyncMergeRow row,
		ILookup<Guid, TransactionEntry> transactions,
		ILookup<Guid, TemplateEntry> templates,
		CancellationToken cancellationToken)
	{
		if (row.Entity is Transaction transaction)
		{
			await SaveTransactionEntries(
				unitOfWork,
				transaction,
				transactions[transaction.Id].ToList(),
				cancellationToken);
		}
		else if (row.Entity is Template template && set.ContentChanged(row))
		{
			await SaveTemplateEntries(
				unitOfWork,
				template,
				templates[template.Id].ToList(),
				cancellationToken);
		}
	}

	private static async Task SaveTransactionEntries(
		IMasterUnitOfWork unitOfWork,
		Transaction transaction,
		List<TransactionEntry> old,
		CancellationToken cancellationToken)
	{
		await unitOfWork.TransactionEntryRepo.RemoveRange(
			old.Where(entry => transaction.Entries.All(current => current.Id != entry.Id)), cancellationToken);
		SaveCurrentEntries(
			unitOfWork.TransactionEntryRepo,
			transaction.Entries,
			old.Select(entry => entry.Id).ToHashSet());
	}

	private static async Task SaveTemplateEntries(
		IMasterUnitOfWork unitOfWork,
		Template template,
		List<TemplateEntry> old,
		CancellationToken cancellationToken)
	{
		await unitOfWork.TemplateEntryRepo.RemoveRange(
			old.Where(entry => template.Entries.All(current => current.Id != entry.Id)), cancellationToken);
		SaveCurrentEntries(
			unitOfWork.TemplateEntryRepo,
			template.Entries,
			old.Select(entry => entry.Id).ToHashSet());
	}

	private static void SaveCurrentEntries<T>(IRepository<T> repository, ICollection<T> entries, HashSet<Guid> oldIds)
		where T : class, IBaseEntity
	{
		foreach (T entry in entries)
		{
			if (oldIds.Contains(entry.Id)) { repository.Update(entry); }
			else { repository.Add(entry); }
		}
	}

	private static Dictionary<string, SyncEntitySet> CreateSets(IMasterUnitOfWork unitOfWork) =>
		CreateCurrencySets(unitOfWork)
			.Concat(CreateGroupSets(unitOfWork))
			.Concat(CreateElementSets(unitOfWork))
			.Concat(CreateAggregateSets(unitOfWork))
			.ToDictionary(set => set.Type, StringComparer.Ordinal);

	private static SyncEntitySet[] CreateCurrencySets(IMasterUnitOfWork unitOfWork) =>
	[
		SyncEntitySet.Create(
			"currency",
			unitOfWork.CurrencyRepo,
			["Code", "Symbol", "EnglishName", "Name", "IsFavorite"]),
		SyncEntitySet.Create(
			"currencyRate",
			unitOfWork.CurrencyRateRepo,
			["CurrencyId", "Date", "Rate", "Description"])
	];

	private static SyncEntitySet[] CreateGroupSets(IMasterUnitOfWork unitOfWork)
	{
		string[] properties = ["Name", "Description", "IsFavorite", "ParentId"];
		return
		[
			SyncEntitySet.Create(
				"accountGroup",
				unitOfWork.AccountGroupRepo,
				properties,
				RootsIds.AccountGroupId,
				"accountGroup",
				"parentId"),
			SyncEntitySet.Create(
				"categoryGroup",
				unitOfWork.CategoryGroupRepo,
				properties,
				RootsIds.CategoryGroupId,
				"categoryGroup",
				"parentId"),
			SyncEntitySet.Create(
				"correspondentGroup",
				unitOfWork.CorrespondentGroupRepo,
				properties,
				RootsIds.CorrespondentGroupId,
				"correspondentGroup",
				"parentId"),
			SyncEntitySet.Create(
				"projectGroup",
				unitOfWork.ProjectGroupRepo,
				properties,
				RootsIds.ProjectGroupId,
				"projectGroup",
				"parentId"),
			SyncEntitySet.Create(
				"templateGroup",
				unitOfWork.TemplateGroupRepo,
				properties,
				RootsIds.TemplateGroupId,
				"templateGroup",
				"parentId"),
			SyncEntitySet.Create(
				"reportGroup",
				unitOfWork.ReportGroupRepo,
				properties,
				RootsIds.ReportGroupId,
				"reportGroup",
				"parentId")
		];
	}

	private static SyncEntitySet[] CreateElementSets(IMasterUnitOfWork unitOfWork)
	{
		string[] properties = ["Name", "Description", "IsFavorite", "GroupId"];
		return
		[
			SyncEntitySet.Create(
				"category",
				unitOfWork.CategoryRepo,
				properties,
				parentType: "categoryGroup",
				parentField: "groupId"),
			SyncEntitySet.Create(
				"correspondent",
				unitOfWork.CorrespondentRepo,
				properties,
				parentType: "correspondentGroup",
				parentField: "groupId"),
			SyncEntitySet.Create(
				"project",
				unitOfWork.ProjectRepo,
				properties,
				parentType: "projectGroup",
				parentField: "groupId"),
			SyncEntitySet.Create(
				"account",
				unitOfWork.AccountRepo,
				[.. properties, "CurrencyId", "CategoryId", "CorrespondentId", "ProjectId"],
				parentType: "accountGroup",
				parentField: "groupId")
		];
	}

	private static SyncEntitySet[] CreateAggregateSets(IMasterUnitOfWork unitOfWork)
	{
		string[] properties = ["Name", "Description", "IsFavorite", "GroupId"];
		return
		[
			SyncEntitySet.Create(
				"template",
				unitOfWork.TemplateRepo,
				[.. properties, "Entries"],
				parentType: "templateGroup",
				parentField: "groupId"),
			SyncEntitySet.Create(
				"report",
				unitOfWork.ReportRepo,
				[.. properties, "Json"],
				parentType: "reportGroup",
				parentField: "groupId"),
			SyncEntitySet.Create(
				"transaction",
				unitOfWork.TransactionRepo,
				["DateTime", "State", "Description", "Entries"])
		];
	}
}
