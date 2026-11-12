using Master.Impl.Operations.MergeSyncBatch;
using Master.Impl.Operations.MapSyncEntity;
using DataAccess.Contracts.Repositories.Base;
using Business.Models.Entities.Reporting;
using Business.Models.Entities.Base;
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
using Master.Impl.Operations.RestoreConsistency;
using Master.Impl.Operations.ValidateSyncBatch;

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
	private readonly IMergeSyncBatchOperation _mergeSyncBatchOperation;
	private readonly IMapSyncEntityOperation _mapSyncEntityOperation;
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;
	private readonly IRestoreConsistencyOperation _restoreConsistencyOperation;
	private readonly IValidateSyncBatchOperation _validateSyncBatchOperation;

	public ApplySyncBatchOperation(IUnitOfWorkFactory unitOfWorkFactory, IMergeSyncBatchOperation mergeSyncBatchOperation, IMapSyncEntityOperation mapSyncEntityOperation,
		IRestoreConsistencyOperation restoreConsistencyOperation, IValidateSyncBatchOperation validateSyncBatchOperation)
	{
		_mergeSyncBatchOperation = mergeSyncBatchOperation;
		_mapSyncEntityOperation = mapSyncEntityOperation;
		_unitOfWorkFactory = unitOfWorkFactory;
		_restoreConsistencyOperation = restoreConsistencyOperation;
		_validateSyncBatchOperation = validateSyncBatchOperation;
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
		Dictionary<string, SyncEntityState> sets = await LoadSets(unitOfWork, cancellationToken);
		(ICollection<TransactionEntry> transactions, ICollection<TemplateEntry> templates) = await LoadEntries(unitOfWork, cancellationToken);
		AttachEntries(sets, transactions, templates);
		_mergeSyncBatchOperation.Run(syncBatch, localRegistrationId, revision, sets, sources, cancellationToken);
		_restoreConsistencyOperation.Run(sets, configuration, cancellationToken);
		_validateSyncBatchOperation.Run(sets, configuration, syncBatch.Priority, cancellationToken);
		List<(SyncEntityState Set, SyncMergeRow Row)> changed = PrepareChanges(sets, revision);
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

	private async Task<Dictionary<string, SyncEntityState>> LoadSets(
		IMasterUnitOfWork unitOfWork,
		CancellationToken cancellationToken)
	{
		Dictionary<string, SyncEntityState> sets = CreateSets();
		foreach (SyncEntityState set in sets.Values)
		{
			await (set.Type switch
			{
				nameof(Currency) => LoadEntities(set, unitOfWork.CurrencyRepo, cancellationToken),
				nameof(CurrencyRate) => LoadEntities(set, unitOfWork.CurrencyRateRepo, cancellationToken),
				nameof(AccountGroup) => LoadEntities(set, unitOfWork.AccountGroupRepo, cancellationToken),
				nameof(CategoryGroup) => LoadEntities(set, unitOfWork.CategoryGroupRepo, cancellationToken),
				nameof(CorrespondentGroup) => LoadEntities(set, unitOfWork.CorrespondentGroupRepo, cancellationToken),
				nameof(ProjectGroup) => LoadEntities(set, unitOfWork.ProjectGroupRepo, cancellationToken),
				nameof(TemplateGroup) => LoadEntities(set, unitOfWork.TemplateGroupRepo, cancellationToken),
				nameof(ReportGroup) => LoadEntities(set, unitOfWork.ReportGroupRepo, cancellationToken),
				nameof(Category) => LoadEntities(set, unitOfWork.CategoryRepo, cancellationToken),
				nameof(Correspondent) => LoadEntities(set, unitOfWork.CorrespondentRepo, cancellationToken),
				nameof(Project) => LoadEntities(set, unitOfWork.ProjectRepo, cancellationToken),
				nameof(Account) => LoadEntities(set, unitOfWork.AccountRepo, cancellationToken),
				nameof(Template) => LoadEntities(set, unitOfWork.TemplateRepo, cancellationToken),
				nameof(Report) => LoadEntities(set, unitOfWork.ReportRepo, cancellationToken),
				nameof(Transaction) => LoadEntities(set, unitOfWork.TransactionRepo, cancellationToken),
				_ => throw new InvalidOperationException("Unsupported synchronization entity type.")
			});
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

	private void AttachEntries(Dictionary<string, SyncEntityState> sets,
		ICollection<TransactionEntry> transactions, ICollection<TemplateEntry> templates)
	{
		AttachTransactionEntries(sets[nameof(Transaction)], transactions.ToLookup(entry => entry.TransactionId));
		AttachTemplateEntries(sets[nameof(Template)], templates.ToLookup(entry => entry.TemplateId));
	}

	private void AttachTransactionEntries(SyncEntityState set, ILookup<Guid, TransactionEntry> entries)
	{
		foreach (SyncMergeRow row in set.Rows.Values)
		{
			((Transaction)row.Entity).Entries = entries[row.Entity.Id].OrderBy(entry => entry.Position).ToList();
			_mapSyncEntityOperation.RememberAggregate(set, row);
		}
	}

	private void AttachTemplateEntries(SyncEntityState set, ILookup<Guid, TemplateEntry> entries)
	{
		foreach (SyncMergeRow row in set.Rows.Values)
		{
			((Template)row.Entity).Entries = entries[row.Entity.Id].OrderBy(entry => entry.Position).ToList();
			_mapSyncEntityOperation.RememberAggregate(set, row);
		}
	}

	private List<(SyncEntityState Set, SyncMergeRow Row)> PrepareChanges(Dictionary<string, SyncEntityState> sets, long revision)
	{
		List<(SyncEntityState Set, SyncMergeRow Row)> changed = new();
		foreach (SyncEntityState set in sets.Values)
		{
			foreach (SyncMergeRow row in set.Rows.Values)
			{
				if (StampChanges(set, row, revision)) { changed.Add((set, row)); }
			}
		}
		return changed;
	}

	private bool StampChanges(SyncEntityState set, SyncMergeRow row, long revision)
	{
		bool contentChanged = _mapSyncEntityOperation.ContentChanged(set, row) || row.IsLive != row.OriginalDeleteRevision is null;
		row.Tracking.EditRevision = contentChanged ? revision : row.OriginalEditRevision;
		row.Tracking.DeleteRevision = row.IsLive ? null : contentChanged ? revision : row.OriginalDeleteRevision;
		row.Tracking.ModificationType = StoredModificationType.None;
		return contentChanged || (row.Entity as IOrderedEntity)?.Order != row.OriginalOrder;
	}

	private static void ValidateEntryOwners(List<(SyncEntityState Set, SyncMergeRow Row)> changed,
		ICollection<TransactionEntry> transactions, ICollection<TemplateEntry> templates)
	{
		Dictionary<Guid, Guid> transactionOwners = transactions.ToDictionary(entry => entry.Id, entry => entry.TransactionId);
		Dictionary<Guid, Guid> templateOwners = templates.ToDictionary(entry => entry.Id, entry => entry.TemplateId);
		foreach ((SyncEntityState _, SyncMergeRow row) in changed)
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

	private async Task PersistChanges(
		IMasterUnitOfWork unitOfWork,
		List<(SyncEntityState Set, SyncMergeRow Row)> changed,
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

	private async Task SaveEntities(
		IMasterUnitOfWork unitOfWork,
		List<(SyncEntityState Set, SyncMergeRow Row)> changed,
		CancellationToken cancellationToken)
	{
		foreach ((SyncEntityState set, SyncMergeRow row) in changed)
		{
			switch (row.Entity)
			{
				case Currency entity: PersistEntity(unitOfWork.CurrencyRepo, entity, row.IsNew); break;
				case CurrencyRate entity: PersistEntity(unitOfWork.CurrencyRateRepo, entity, row.IsNew); break;
				case AccountGroup entity: PersistEntity(unitOfWork.AccountGroupRepo, entity, row.IsNew); break;
				case CategoryGroup entity: PersistEntity(unitOfWork.CategoryGroupRepo, entity, row.IsNew); break;
				case CorrespondentGroup entity: PersistEntity(unitOfWork.CorrespondentGroupRepo, entity, row.IsNew); break;
				case ProjectGroup entity: PersistEntity(unitOfWork.ProjectGroupRepo, entity, row.IsNew); break;
				case TemplateGroup entity: PersistEntity(unitOfWork.TemplateGroupRepo, entity, row.IsNew); break;
				case ReportGroup entity: PersistEntity(unitOfWork.ReportGroupRepo, entity, row.IsNew); break;
				case Category entity: PersistEntity(unitOfWork.CategoryRepo, entity, row.IsNew); break;
				case Correspondent entity: PersistEntity(unitOfWork.CorrespondentRepo, entity, row.IsNew); break;
				case Project entity: PersistEntity(unitOfWork.ProjectRepo, entity, row.IsNew); break;
				case Account entity: PersistEntity(unitOfWork.AccountRepo, entity, row.IsNew); break;
				case Template entity: PersistEntity(unitOfWork.TemplateRepo, entity, row.IsNew); break;
				case Report entity: PersistEntity(unitOfWork.ReportRepo, entity, row.IsNew); break;
				case Transaction entity: PersistEntity(unitOfWork.TransactionRepo, entity, row.IsNew); break;
				default: throw new InvalidOperationException("Unsupported synchronization entity type.");
			}
		}
		await unitOfWork.SaveChanges(cancellationToken);
	}

	private async Task SaveEntries(
		IMasterUnitOfWork unitOfWork,
		List<(SyncEntityState Set, SyncMergeRow Row)> changed,
		ICollection<TransactionEntry> transactions,
		ICollection<TemplateEntry> templates,
		CancellationToken cancellationToken)
	{
		ILookup<Guid, TransactionEntry> transactionsByParent = transactions.ToLookup(entry => entry.TransactionId);
		ILookup<Guid, TemplateEntry> templatesByParent = templates.ToLookup(entry => entry.TemplateId);
		foreach ((SyncEntityState set, SyncMergeRow row) in changed)
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

	private async Task SaveAggregateEntries(
		IMasterUnitOfWork unitOfWork,
		SyncEntityState set,
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
		else if (row.Entity is Template template && _mapSyncEntityOperation.ContentChanged(set, row))
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

	private static Dictionary<string, SyncEntityState> CreateSets() =>
		CreateCurrencySets()
			.Concat(CreateGroupSets())
			.Concat(CreateElementSets())
			.Concat(CreateAggregateSets())
			.ToDictionary(set => set.Type, StringComparer.Ordinal);

	private static SyncEntityState[] CreateCurrencySets() =>
	[
		CreateState<Currency>(nameof(Currency),
			[nameof(Currency.Code), nameof(Currency.Symbol), nameof(Currency.EnglishName), nameof(Currency.Name), nameof(Currency.IsFavorite)]),
		CreateState<CurrencyRate>(nameof(CurrencyRate),
			[nameof(CurrencyRate.CurrencyId), nameof(CurrencyRate.Date), nameof(CurrencyRate.Rate), nameof(CurrencyRate.Description)])
	];

	private static SyncEntityState[] CreateGroupSets()
	{
		string[] properties = [nameof(CatalogEntity.Name), nameof(CatalogEntity.Description), nameof(CatalogEntity.IsFavorite)];
		return
		[
			CreateState<AccountGroup>(nameof(AccountGroup),
				[.. properties, nameof(AccountGroup.ParentId)],
				RootsIds.AccountGroupId,
				nameof(AccountGroup),
				nameof(AccountGroup.ParentId)),
			CreateState<CategoryGroup>(nameof(CategoryGroup),
				[.. properties, nameof(CategoryGroup.ParentId)],
				RootsIds.CategoryGroupId,
				nameof(CategoryGroup),
				nameof(CategoryGroup.ParentId)),
			CreateState<CorrespondentGroup>(nameof(CorrespondentGroup),
				[.. properties, nameof(CorrespondentGroup.ParentId)],
				RootsIds.CorrespondentGroupId,
				nameof(CorrespondentGroup),
				nameof(CorrespondentGroup.ParentId)),
			CreateState<ProjectGroup>(nameof(ProjectGroup),
				[.. properties, nameof(ProjectGroup.ParentId)],
				RootsIds.ProjectGroupId,
				nameof(ProjectGroup),
				nameof(ProjectGroup.ParentId)),
			CreateState<TemplateGroup>(nameof(TemplateGroup),
				[.. properties, nameof(TemplateGroup.ParentId)],
				RootsIds.TemplateGroupId,
				nameof(TemplateGroup),
				nameof(TemplateGroup.ParentId)),
			CreateState<ReportGroup>(nameof(ReportGroup),
				[.. properties, nameof(ReportGroup.ParentId)],
				RootsIds.ReportGroupId,
				nameof(ReportGroup),
				nameof(ReportGroup.ParentId))
		];
	}

	private static SyncEntityState[] CreateElementSets()
	{
		string[] properties = [nameof(CatalogEntity.Name), nameof(CatalogEntity.Description), nameof(CatalogEntity.IsFavorite)];
		return
		[
			CreateState<Category>(nameof(Category),
				[.. properties, nameof(Category.GroupId)],
				parentType: nameof(CategoryGroup),
				parentField: nameof(Category.GroupId)),
			CreateState<Correspondent>(nameof(Correspondent),
				[.. properties, nameof(Correspondent.GroupId)],
				parentType: nameof(CorrespondentGroup),
				parentField: nameof(Correspondent.GroupId)),
			CreateState<Project>(nameof(Project),
				[.. properties, nameof(Project.GroupId)],
				parentType: nameof(ProjectGroup),
				parentField: nameof(Project.GroupId)),
			CreateState<Account>(nameof(Account),
				[.. properties, nameof(Account.GroupId), nameof(Account.CurrencyId), nameof(Account.CategoryId), nameof(Account.CorrespondentId), nameof(Account.ProjectId)],
				parentType: nameof(AccountGroup),
				parentField: nameof(Account.GroupId))
		];
	}

	private static SyncEntityState[] CreateAggregateSets()
	{
		string[] properties = [nameof(CatalogEntity.Name), nameof(CatalogEntity.Description), nameof(CatalogEntity.IsFavorite)];
		return
		[
			CreateState<Template>(nameof(Template),
				[.. properties, nameof(Template.GroupId), nameof(Template.Entries)],
				parentType: nameof(TemplateGroup),
				parentField: nameof(Template.GroupId)),
			CreateState<Report>(nameof(Report),
				[.. properties, nameof(Report.GroupId), nameof(Report.Json)],
				parentType: nameof(ReportGroup),
				parentField: nameof(Report.GroupId)),
			CreateState<Transaction>(nameof(Transaction),
				[nameof(Transaction.DateTime), nameof(Transaction.State), nameof(Transaction.Description), nameof(Transaction.Entries)])
		];
	}
	private async Task LoadEntities<T>(SyncEntityState set, ITrackedRepository<T> repository, CancellationToken cancellationToken)
		where T : BaseEntity, ITrackedEntity
	{
		foreach (T entity in await repository.GetAll(cancellationToken))
		{
			set.Rows.Add(entity.Id, new SyncMergeRow
			{
				Entity = entity, Original = _mapSyncEntityOperation.Snapshot(set, entity),
				OriginalEditRevision = entity.EditRevision, OriginalDeleteRevision = entity.DeleteRevision,
				OriginalOrder = (entity as IOrderedEntity)?.Order
			});
		}
	}

	private static void PersistEntity<T>(ITrackedRepository<T> repository, T entity, bool isNew)
		where T : BaseEntity, ITrackedEntity
	{
		if (isNew) { repository.Add(entity); }
		else { repository.Update(entity); }
	}

	private static SyncEntityState CreateState<T>(string type, string[] fields,
		Guid? rootId = null, string? parentType = null, string? parentField = null)
		where T : BaseEntity, ITrackedEntity
		=> new()
		{
			Type = type, EntityType = typeof(T), ContentFields = fields,
			RootId = rootId, ParentType = parentType, ParentField = parentField,
			IsOrdered = typeof(IOrderedEntity).IsAssignableFrom(typeof(T))
		};
}
