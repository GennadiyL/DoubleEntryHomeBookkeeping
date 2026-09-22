using Business.Contracts.Params;
using Business.Contracts.Services.Base;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Shared.Contracts;

namespace Business.Impl.Services.Base;

public abstract class ElementService<TGroup, TElement> : IElementService<TGroup, TElement, ElementParam>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : ElementEntity<TGroup, TElement>, new()
{
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IElementRepository<TGroup, TElement> _repository;
	private readonly IGroupRepository<TGroup, TElement> _groupRepository;

	protected ElementService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork,
		IElementRepository<TGroup, TElement> repository, IGroupRepository<TGroup, TElement> groupRepository)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_repository = repository;
		_groupRepository = groupRepository;
	}

	public async Task<Guid> Add(ElementParam param)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element name and group identifier are required.");
		}

		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(param.GroupId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (group.Elements.Any(element => string.Equals(element.Name, param.Name, StringComparison.Ordinal)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		int maxOrder = group.Elements.Count == 0 ? 0 : group.Elements.Max(element => element.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The group has no available element ordering position.");
		}

		DateTime now = _sharedContext.DateTimeService.UtcNow;
		TElement element = new()
		{
			Id = Guid.NewGuid(),
			Name = param.Name,
			Description = param.Description,
			IsFavorite = param.IsFavorite,
			GroupId = group.Id,
			Group = group,
			Order = maxOrder + 1,
			Original = now,
			Current = now
		};
		_repository.Add(element);
		await _unitOfWork.SaveChangesAsync();
		return element.Id;
	}

	public async Task Update(Guid entityId, ElementParam param)
	{
		if (entityId == Guid.Empty || param is null ||
			string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier, name, and group identifier are required.");
		}

		TElement element = await GetActiveElement(entityId);
		if (param.GroupId != element.GroupId)
		{
			throw new InvalidElementException("Use MoveToAnotherGroup to change the group.");
		}

		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(element.GroupId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		if (group.Elements.Any(item => item.Id != element.Id &&
			string.Equals(item.Name, param.Name, StringComparison.Ordinal)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		element.Name = param.Name;
		element.Description = param.Description;
		element.IsFavorite = param.IsFavorite;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task Delete(Guid entityId)
	{
		TElement element = await GetActiveElement(entityId);
		element.IsDeleted = true;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task SetOrder(Guid entityId, int order)
	{
		if (order < 1)
		{
			throw new InvalidElementException("A positive order is required.");
		}

		TElement element = await GetActiveElement(entityId);
		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(element.GroupId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		List<TElement> siblings = [.. group.Elements.Where(item => !item.IsDeleted)
			.OrderBy(item => item.Order).ThenBy(item => item.Id)];
		TElement? target = siblings.SingleOrDefault(item => item.Id == entityId);
		if (target is null)
		{
			throw new ElementNotFoundException("The element is no longer an active member of this group.");
		}
		if (order > siblings.Count)
		{
			throw new InvalidElementException("The order exceeds the number of active elements in this group.");
		}

		Dictionary<Guid, int> originalOrders = siblings.ToDictionary(item => item.Id, item => item.Order);
		siblings.Reorder();
		siblings.SetOrder(target, order);
		List<TElement> changed = [.. siblings.Where(item => item.Order != originalOrders[item.Id])];
		if (changed.Count == 0)
		{
			return;
		}

		DateTime now = _sharedContext.DateTimeService.UtcNow;
		foreach (TElement sibling in changed)
		{
			sibling.Current = now;
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite)
	{
		TElement element = await GetActiveElement(entityId);
		if (element.IsFavorite == isFavorite)
		{
			return;
		}

		element.IsFavorite = isFavorite;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task MoveToAnotherGroup(Guid entityId, Guid toGroupId)
	{
		if (entityId == Guid.Empty || toGroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier and destination group identifier are required.");
		}

		TElement element = await GetActiveElement(entityId);
		TGroup? destination = await _groupRepository.GetWithContentsByIdAsync(toGroupId);
		if (destination is null || destination.IsDeleted)
		{
			throw new GroupNotFoundException("The destination group does not exist or is deleted.");
		}
		if (element.GroupId == toGroupId)
		{
			return;
		}
		if (destination.Elements.Any(item => string.Equals(item.Name, element.Name, StringComparison.Ordinal)))
		{
			throw new InvalidElementException("An element with the same name already exists in the destination group.");
		}

		int maxOrder = destination.Elements.Count == 0 ? 0 : destination.Elements.Max(item => item.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The destination group has no available element ordering position.");
		}

		element.GroupId = destination.Id;
		element.Group = destination;
		element.Order = maxOrder + 1;
		element.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task CombineElements(Guid toElementId, Guid fromElementId)
	{
		if (toElementId == Guid.Empty || fromElementId == Guid.Empty || toElementId == fromElementId)
		{
			throw new InvalidElementException("Two different element identifiers are required.");
		}

		TElement source = await GetActiveElement(fromElementId);
		TElement destination = await GetActiveElement(toElementId);
		ICollection<Account> accounts = await GetReferencingAccounts(fromElementId);
		DateTime now = _sharedContext.DateTimeService.UtcNow;
		foreach (Account account in accounts)
		{
			ReplaceAccountReference(account, destination);
			account.Current = now;
			_unitOfWork.AccountRepo.Update(account);
		}

		source.IsDeleted = true;
		source.Current = now;
		_repository.Update(source);
		await _unitOfWork.SaveChangesAsync();
	}

	private async Task<TElement> GetActiveElement(Guid entityId)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier is required.");
		}

		TElement? element = await _repository.GetByIdAsync(entityId, CancellationToken.None);
		if (element is null || element.IsDeleted)
		{
			throw new ElementNotFoundException("The element does not exist or is deleted.");
		}
		return element;
	}

	protected abstract Task<ICollection<Account>> GetReferencingAccounts(Guid elementId);

	protected abstract void ReplaceAccountReference(Account account, TElement destination);
}
