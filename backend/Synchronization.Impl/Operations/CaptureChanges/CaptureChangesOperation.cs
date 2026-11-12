using Business.Models.Constants;
using Business.Models.Entities.Reporting;
using Business.Models.Entities;
using System.Text.Json;
using Business.Core.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Config;
using Business.Models.Entities.Interfaces;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Exchange.Models.Synchronizations;
using Shared.Contracts;
using Business.Models.Enums;
using ExchangeModificationType = Exchange.Models.Enums.ModificationType;
using ExchangeConflictPriority = Exchange.Models.Enums.ConflictPriority;

namespace Synchronization.Impl.Operations.CaptureChanges;

/// <summary>
/// Builds a detached synchronization batch from uncaptured Local changes.
/// Reads tracked repositories sequentially through the current unit of work.
/// Captures a new identity, UTC time and the current Local conflict priority.
/// Explicit mappings exclude navigation graphs, Local settings and derived caches.
/// Transactions and templates carry their owned entries in stored position order.
/// Never-captured deleted creations are included as full-content deletions.
/// Reads do not clear flags, change revisions, delete rows or persist the batch.
/// The caller must prevent concurrent edits and coordinate durable capture acceptance.
/// </summary>
internal sealed class CaptureChangesOperation : ICaptureChangesOperation
{
	private readonly IUnitOfWorkFactory _unitOfWorkFactory;
	private readonly ISharedContext _sharedContext;

	public CaptureChangesOperation(ISharedContext sharedContext, IUnitOfWorkFactory unitOfWorkFactory)
	{
		_sharedContext = sharedContext;
		_unitOfWorkFactory = unitOfWorkFactory;
	}

	public async Task<SyncBatch> Run(CancellationToken cancellationToken = default)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ILocalUnitOfWork unitOfWork = _unitOfWorkFactory.CreateLocal();
		ICollection<LocalConfig> configurations = await unitOfWork.LocalConfigRepo.GetAll(cancellationToken);
		if (configurations.Count != 1)
		{
			throw new InvalidOperationException("The Local configuration singleton is missing or invalid.");
		}
		ExchangeConflictPriority priority = configurations.Single().ConflictPriority switch
		{
			ConflictPriority.Local => ExchangeConflictPriority.Local,
			ConflictPriority.Master => ExchangeConflictPriority.Master,
			_ => throw new InvalidOperationException("The Local conflict priority is invalid.")
		};
		SyncBatch batch = new()
		{
			SyncKey = Guid.NewGuid().ToString("N"),
			CreatedAt = _sharedContext.DateTimeService.UtcNow,
			Priority = priority
		};
		await Capture(unitOfWork.CurrencyRepo, nameof(Currency), item => Properties(
			(nameof(item.Code), item.Code), (nameof(item.Symbol), item.Symbol), (nameof(item.EnglishName), item.EnglishName),
			(nameof(item.Name), item.Name), (nameof(item.IsFavorite), item.IsFavorite)), batch, cancellationToken);
		await Capture(unitOfWork.CurrencyRateRepo, nameof(CurrencyRate), item => Properties(
			(nameof(item.CurrencyId), item.CurrencyId), (nameof(item.Date), item.Date), (nameof(item.Rate), item.Rate),
			(nameof(item.Description), item.Description)), batch, cancellationToken);
		await Capture(unitOfWork.AccountGroupRepo, nameof(AccountGroup), GroupProperties, batch, cancellationToken);
		await Capture(unitOfWork.CategoryGroupRepo, nameof(CategoryGroup), GroupProperties, batch, cancellationToken);
		await Capture(unitOfWork.CorrespondentGroupRepo, nameof(CorrespondentGroup), GroupProperties, batch, cancellationToken);
		await Capture(unitOfWork.ProjectGroupRepo, nameof(ProjectGroup), GroupProperties, batch, cancellationToken);
		await Capture(unitOfWork.TemplateGroupRepo, nameof(TemplateGroup), GroupProperties, batch, cancellationToken);
		await Capture(unitOfWork.ReportGroupRepo, nameof(ReportGroup), GroupProperties, batch, cancellationToken);
		await Capture(unitOfWork.CategoryRepo, nameof(Category), ElementProperties, batch, cancellationToken);
		await Capture(unitOfWork.CorrespondentRepo, nameof(Correspondent), ElementProperties, batch, cancellationToken);
		await Capture(unitOfWork.ProjectRepo, nameof(Project), ElementProperties, batch, cancellationToken);
		await Capture(unitOfWork.AccountRepo, nameof(Account), item =>
		{
			Dictionary<string, object?> properties = ElementProperties(item);
			properties.Add(nameof(item.CurrencyId), item.CurrencyId);
			properties.Add(nameof(item.CategoryId), item.CategoryId);
			properties.Add(nameof(item.CorrespondentId), item.CorrespondentId);
			properties.Add(nameof(item.ProjectId), item.ProjectId);
			return properties;
		}, batch, cancellationToken);
		await Capture(unitOfWork.TemplateRepo, nameof(Template), item =>
		{
			Dictionary<string, object?> properties = ElementProperties(item);
			properties.Add(nameof(item.Entries), item.Entries.OrderBy(entry => entry.Position).Select(entry => Properties(
				(nameof(entry.Id), entry.Id), (nameof(entry.AccountId), entry.AccountId), (nameof(entry.Position), entry.Position),
				(nameof(entry.Amount), entry.Amount))).ToArray());
			return properties;
		}, batch, cancellationToken);
		await Capture(unitOfWork.TransactionRepo, nameof(Transaction), item => Properties(
			(nameof(item.DateTime), item.DateTime), (nameof(item.State), item.State), (nameof(item.Description), item.Description),
			(nameof(item.Entries), item.Entries.OrderBy(entry => entry.Position).Select(entry => Properties(
				(nameof(entry.Id), entry.Id), (nameof(entry.AccountId), entry.AccountId), (nameof(entry.Position), entry.Position),
				(nameof(entry.Amount), entry.Amount), (nameof(entry.Rate), entry.Rate))).ToArray())), batch, cancellationToken);
		await Capture(unitOfWork.ReportRepo, nameof(Report), item =>
		{
			Dictionary<string, object?> properties = ElementProperties(item);
			properties.Add(nameof(item.Json), item.Json);
			return properties;
		}, batch, cancellationToken);
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
			bool deleted = entity.DeleteRevision == AppValues.LocalDeleteRevision;
			if (entity.DeleteRevision > AppValues.LocalDeleteRevision ||
				(!added && !deleted && entity.ModificationType == ModificationType.None))
			{
				continue;
			}
			bool hasContent = (entity.ModificationType & ModificationType.Content) != ModificationType.None;
			bool hasOrder = (entity.ModificationType & ModificationType.Order) != ModificationType.None;
			if (hasOrder && entity is not IOrderedEntity)
			{
				throw new InvalidOperationException("An unordered entity has an ordering modification.");
			}
			Dictionary<string, object?> properties = added || deleted || hasContent
				? content(entity)
				: new Dictionary<string, object?>();
			if (entity is IOrderedEntity ordered && (added || deleted || hasOrder))
			{
				properties.Add(nameof(IOrderedEntity.Order), ordered.Order);
			}
			ExchangeModificationType flags = ExchangeModificationType.None;
			if (hasContent || deleted)
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
		=> Properties((nameof(entity.Name), entity.Name), (nameof(entity.Description), entity.Description), (nameof(entity.IsFavorite), entity.IsFavorite));

	private static Dictionary<string, object?> GroupProperties<TGroup, TElement>(GroupEntity<TGroup, TElement> entity)
		where TGroup : class, IGroupEntity<TGroup, TElement>
		where TElement : class, IElementEntity<TGroup, TElement>
	{
		Dictionary<string, object?> properties = CatalogProperties(entity);
		properties.Add(nameof(entity.ParentId), entity.ParentId);
		return properties;
	}

	private static Dictionary<string, object?> ElementProperties<TGroup, TElement>(ElementEntity<TGroup, TElement> entity)
		where TGroup : class, IGroupEntity<TGroup, TElement>
		where TElement : class, IElementEntity<TGroup, TElement>
	{
		Dictionary<string, object?> properties = CatalogProperties(entity);
		properties.Add(nameof(entity.GroupId), entity.GroupId);
		return properties;
	}
}
