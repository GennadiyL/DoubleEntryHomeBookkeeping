using System.Text.Json;
using Business.Core.Entities;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Exchange.Models.Synchronizations;
using Shared.Contracts;
using ModificationType = Business.Models.Enums.ModificationType;
using ExchangeModificationType = Exchange.Models.Enums.ModificationType;
using ExchangeConflictPriority = Exchange.Models.Enums.ConflictPriority;

namespace Synchronization.Impl.Operations.CaptureChanges;

/// <summary>
/// Builds a detached synchronization batch from uncaptured Local changes.
/// Reads tracked repositories sequentially through the current unit of work.
/// Captures a new identity, UTC time and the current Local conflict priority.
/// Explicit mappings exclude navigation graphs, Local settings and derived caches.
/// Transactions and templates carry their owned entries in stored position order.
/// Never-captured creations already deleted locally are omitted for later cleanup.
/// Reads do not clear flags, change revisions, delete rows or persist the batch.
/// The caller must prevent concurrent edits and coordinate durable capture acceptance.
/// </summary>
internal sealed class CaptureChangesOperation : ICaptureChangesOperation
{
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly ISharedContext _sharedContext;

	public CaptureChangesOperation(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
	}

	public async Task<SyncBatch> Run(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ICollection<LocalConfig> configurations = await _unitOfWork.LocalConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1)
		{
			throw new InvalidOperationException("The Local configuration singleton is missing or invalid.");
		}
		ExchangeConflictPriority priority = configurations.Single().ConflictPriority switch
		{
			Business.Models.Enums.ConflictPriority.Local => ExchangeConflictPriority.Local,
			Business.Models.Enums.ConflictPriority.Master => ExchangeConflictPriority.Master,
			_ => throw new InvalidOperationException("The Local conflict priority is invalid.")
		};
		SyncBatch batch = new()
		{
			SyncKey = Guid.NewGuid().ToString("N"),
			CreatedAt = _sharedContext.DateTimeService.UtcNow,
			Priority = priority
		};
		await Capture(_unitOfWork.CurrencyRepo, "currency", item => Properties(
			("code", item.Code), ("symbol", item.Symbol), ("englishName", item.EnglishName),
			("name", item.Name), ("isFavorite", item.IsFavorite)), batch, cancellationToken);
		await Capture(_unitOfWork.CurrencyRateRepo, "currencyRate", item => Properties(
			("currencyId", item.CurrencyId), ("date", item.Date), ("rate", item.Rate),
			("description", item.Description)), batch, cancellationToken);
		await Capture(_unitOfWork.AccountGroupRepo, "accountGroup", item => GroupProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.CategoryGroupRepo, "categoryGroup", item => GroupProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.CorrespondentGroupRepo, "correspondentGroup", item => GroupProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.ProjectGroupRepo, "projectGroup", item => GroupProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.TemplateGroupRepo, "templateGroup", item => GroupProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.ReportGroupRepo, "reportGroup", item => GroupProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.CategoryRepo, "category", item => ElementProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.CorrespondentRepo, "correspondent", item => ElementProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.ProjectRepo, "project", item => ElementProperties(item), batch, cancellationToken);
		await Capture(_unitOfWork.AccountRepo, "account", item =>
		{
			Dictionary<string, object?> properties = ElementProperties(item);
			properties.Add("currencyId", item.CurrencyId);
			properties.Add("categoryId", item.CategoryId);
			properties.Add("correspondentId", item.CorrespondentId);
			properties.Add("projectId", item.ProjectId);
			return properties;
		}, batch, cancellationToken);
		await Capture(_unitOfWork.TemplateRepo, "template", item =>
		{
			Dictionary<string, object?> properties = ElementProperties(item);
			properties.Add("entries", item.Entries.OrderBy(entry => entry.Position).Select(entry => Properties(
				("id", entry.Id), ("accountId", entry.AccountId), ("position", entry.Position),
				("amount", entry.Amount))).ToArray());
			return properties;
		}, batch, cancellationToken);
		await Capture(_unitOfWork.TransactionRepo, "transaction", item => Properties(
			("dateTime", item.DateTime), ("state", item.State), ("description", item.Description),
			("entries", item.Entries.OrderBy(entry => entry.Position).Select(entry => Properties(
				("id", entry.Id), ("accountId", entry.AccountId), ("position", entry.Position),
				("amount", entry.Amount), ("rate", entry.Rate))).ToArray())), batch, cancellationToken);
		await Capture(_unitOfWork.ReportRepo, "report", item =>
		{
			Dictionary<string, object?> properties = ElementProperties(item);
			properties.Add("json", item.Json);
			return properties;
		}, batch, cancellationToken);
		await Capture(_unitOfWork.SystemConfigRepo, "systemConfig", item => Properties(
			("masterDatasetKey", item.MasterDatasetKey), ("baseCurrencyId", item.BaseCurrencyId),
			("balancingAccountId", item.BalancingAccountId), ("amountPrecision", item.AmountPrecision),
			("ratePrecision", item.RatePrecision)), batch, cancellationToken);
		return batch;
	}

	private async Task Capture<T>(ITrackedRepository<T> repository, string type,
		Func<T, Dictionary<string, object?>> content, SyncBatch batch, CancellationToken cancellationToken)
		where T : class, IBaseEntity, ITrackedEntity
	{
		ICollection<T> entities = await repository.GetChanges(cancellationToken);
		foreach (T entity in entities)
		{
			cancellationToken.ThrowIfCancellationRequested();
			if (entity.EditRevision < 0 || entity.DeleteRevision < 0 ||
				(entity.ModificationType & ~(ModificationType.Content | ModificationType.Order)) != ModificationType.None)
			{
				throw new InvalidOperationException("Invalid synchronization metadata.");
			}
			bool added = entity.EditRevision is null;
			bool deleted = entity.DeleteRevision == 0;
			if (entity.DeleteRevision > 0 || (added && deleted) ||
				(!added && entity.ModificationType == ModificationType.None))
			{
				continue;
			}
			bool hasContent = (entity.ModificationType & ModificationType.Content) != ModificationType.None;
			bool hasOrder = (entity.ModificationType & ModificationType.Order) != ModificationType.None;
			if (hasOrder && entity is not IOrderedEntity)
			{
				throw new InvalidOperationException("An unordered entity has an ordering modification.");
			}
			if (deleted && !hasContent)
			{
				throw new InvalidOperationException("An uncaptured deletion must have a content modification.");
			}
			Dictionary<string, object?> properties = added || deleted || hasContent
				? content(entity)
				: new Dictionary<string, object?>();
			if (entity is IOrderedEntity ordered && (added || deleted || hasOrder))
			{
				properties.Add("order", ordered.Order);
			}
			ExchangeModificationType flags = ExchangeModificationType.None;
			if (hasContent)
			{
				flags |= ExchangeModificationType.Content;
			}
			if (hasOrder)
			{
				flags |= ExchangeModificationType.Order;
			}
			SyncObject item = new()
			{
				Type = type,
				Id = entity.Id,
				EditRevision = entity.EditRevision,
				ModificationType = flags
			};
			string json = _sharedContext.JsonService.Serialize(properties);
			Dictionary<string, JsonElement> values = _sharedContext.JsonService.Deserialize<Dictionary<string, JsonElement>>(json)
				?? throw new InvalidOperationException("Captured properties could not be serialized.");
			foreach (KeyValuePair<string, JsonElement> property in values)
			{
				item.Properties.Add(property.Key, property.Value.Clone());
			}
			if (deleted)
			{
				batch.Deleted.Add(item);
			}
			else if (added)
			{
				batch.Added.Add(item);
			}
			else
			{
				batch.Updated.Add(item);
			}
		}
	}

	private static Dictionary<string, object?> Properties(params (string Name, object? Value)[] values)
		=> values.ToDictionary(value => value.Name, value => value.Value);

	private static Dictionary<string, object?> CatalogProperties(CatalogEntity entity)
		=> Properties(("name", entity.Name), ("description", entity.Description), ("isFavorite", entity.IsFavorite));

	private static Dictionary<string, object?> GroupProperties<TGroup, TElement>(GroupEntity<TGroup, TElement> entity)
		where TGroup : class, IGroupEntity<TGroup, TElement>
		where TElement : class, IElementEntity<TGroup, TElement>
	{
		Dictionary<string, object?> properties = CatalogProperties(entity);
		properties.Add("parentId", entity.ParentId);
		return properties;
	}

	private static Dictionary<string, object?> ElementProperties<TGroup, TElement>(ElementEntity<TGroup, TElement> entity)
		where TGroup : class, IGroupEntity<TGroup, TElement>
		where TElement : class, IElementEntity<TGroup, TElement>
	{
		Dictionary<string, object?> properties = CatalogProperties(entity);
		properties.Add("groupId", entity.GroupId);
		return properties;
	}
}