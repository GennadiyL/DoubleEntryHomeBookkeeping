using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Contracts.Utils.Merging;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Shared.Contracts;

namespace Business.Impl.Services.Base;

public abstract class ElementService<TGroup, TElement> :
	IElementService<TGroup, TElement>,
	IUpdateEntityService<ElementParam>,
	IReadEntityService<ElementInfo>
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

	public async Task<Guid> Add(ElementParam param, CancellationToken cancellationToken = default)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element name and group identifier are required.");
		}

		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(param.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new InvalidElementException("The group does not exist or is deleted.");
		}

		if (group.Elements.Any(element => string.Equals(element.Name.Trim(), param.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		int maxOrder = group.Elements.Where(element => !element.IsDeleted()).Select(element => element.Order).DefaultIfEmpty(-1).Max();
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidElementException("The group has no available element ordering position.");
		}

		TElement element = new()
		{
			Id = Guid.NewGuid(),
			Name = param.Name.Trim(),
			Description = param.Description,
			IsFavorite = param.IsFavorite,
			GroupId = group.Id,
			Group = group,
			Order = maxOrder + 1,
			EditRevision = null,
			DeleteRevision = null,
			ModificationType = ModificationType.None,
		};
		_repository.Add(element);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
		return element.Id;
	}

	public async Task Update(Guid entityId, ElementParam param, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty || param is null ||
			string.IsNullOrWhiteSpace(param.Name) || param.GroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier, name, and group identifier are required.");
		}

		TElement element = await GetActiveElement(entityId, cancellationToken);
		if (param.GroupId != element.GroupId)
		{
			throw new InvalidElementException("Use MoveToAnotherGroup to change the group.");
		}

		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new InvalidElementException("The group does not exist or is deleted.");
		}
		if (group.Elements.Any(item => item.Id != element.Id &&
			string.Equals(item.Name.Trim(), param.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidElementException("An element with the same name already exists in this group.");
		}

		element.Name = param.Name.Trim();
		element.Description = param.Description;
		element.IsFavorite = param.IsFavorite;
		element.SetEditedContent();
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task Delete(Guid entityId, CancellationToken cancellationToken = default)
	{
		TElement element = await GetActiveElement(entityId, cancellationToken);
		ICollection<Account> accounts = await GetReferencingAccounts(entityId, cancellationToken);
		if (accounts.Count != 0)
		{
			throw new InvalidElementException("The element is referenced by an account.");
		}
		TGroup group = await GetActiveGroup(element.GroupId, cancellationToken);
		element.SetDeleted();
		_repository.Update(element);
		NormalizeElements(group.Elements.Where(item => item.Id != element.Id && !item.IsDeleted()));
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task SetOrder(Guid entityId, int order, CancellationToken cancellationToken = default)
	{
		if (order < 0)
		{
			throw new InvalidElementException("A non-negative order is required.");
		}

		TElement element = await GetActiveElement(entityId, cancellationToken);
		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		List<TElement> siblings = [.. group.Elements.Where(item => !item.IsDeleted())
			.OrderBy(item => item.Order).ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		TElement? target = siblings.SingleOrDefault(item => item.Id == entityId);
		if (target is null)
		{
			throw new ElementNotFoundException("The element is no longer an active member of this group.");
		}
		if (order >= siblings.Count)
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

		foreach (TElement sibling in changed)
		{
			sibling.SetEditedOrder();
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite, CancellationToken cancellationToken = default)
	{
		TElement element = await GetActiveElement(entityId, cancellationToken);
		if (element.IsFavorite == isFavorite)
		{
			return;
		}

		element.IsFavorite = isFavorite;
		element.SetEditedContent();
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task MoveToAnotherGroup(Guid entityId, Guid toGroupId, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty || toGroupId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier and destination group identifier are required.");
		}

		TElement element = await GetActiveElement(entityId, cancellationToken);
		TGroup? destination = await _groupRepository.GetWithContentsByIdAsync(toGroupId, cancellationToken);
		if (destination is null || destination.IsDeleted())
		{
			throw new InvalidElementException("The destination group does not exist or is deleted.");
		}
		if (element.GroupId == toGroupId)
		{
			return;
		}
		if (destination.Elements.Any(item =>
			string.Equals(item.Name.Trim(), element.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidElementException("An element with the same name already exists in the destination group.");
		}

		TGroup source = await GetActiveGroup(element.GroupId, cancellationToken);
		List<TElement> destinationElements = [.. destination.Elements.Where(item => !item.IsDeleted())
			.OrderBy(item => item.Order).ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		NormalizeElements(source.Elements.Where(item => item.Id != element.Id && !item.IsDeleted()));
		NormalizeElements(destinationElements);
		element.GroupId = destination.Id;
		element.Group = destination;
		element.Order = destinationElements.Count;
		element.SetEditedContent();
		element.SetEditedOrder();
		_repository.Update(element);
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	public async Task CombineElements(Guid toElementId, Guid fromElementId, CancellationToken cancellationToken = default)
	{
		if (toElementId == fromElementId)
		{
			return;
		}
		if (toElementId == Guid.Empty || fromElementId == Guid.Empty)
		{
			throw new InvalidElementException("Two element identifiers are required.");
		}

		TElement source = await GetActiveElement(fromElementId, cancellationToken);
		TElement destination = await GetActiveElement(toElementId, cancellationToken);
		TGroup group = await GetActiveGroup(source.GroupId, cancellationToken);
		ICollection<Account> accounts = await GetReferencingAccounts(fromElementId, cancellationToken);
		foreach (Account account in accounts)
		{
			ReplaceAccountReference(account, destination);
			account.SetEditedContent();
			_unitOfWork.AccountRepo.Update(account);
		}

		source.SetDeleted();
		source.SetEditedContent();
		_repository.Update(source);
		NormalizeElements(group.Elements.Where(item => item.Id != source.Id && !item.IsDeleted()));
		await _unitOfWork.SaveChangesAsync(cancellationToken);
	}

	private async Task<TGroup> GetActiveGroup(Guid groupId, CancellationToken cancellationToken = default)
	{
		TGroup? group = await _groupRepository.GetWithContentsByIdAsync(groupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		return group;
	}

	private void NormalizeElements(IEnumerable<TElement> elements)
	{
		List<TElement> ordered = [.. elements.OrderBy(item => item.Order)
			.ThenBy(item => item.Id.ToString("D"), StringComparer.Ordinal)];
		for (int index = 0; index < ordered.Count; index++)
		{
			TElement item = ordered[index];
			if (item.Order == index)
			{
				continue;
			}
			item.Order = index;
			item.SetEditedOrder();
			_repository.Update(item);
		}
	}

	private async Task<TElement> GetActiveElement(Guid entityId, CancellationToken cancellationToken = default)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidElementException("An element identifier is required.");
		}

		TElement? element = await _repository.GetByIdAsync(entityId, cancellationToken);
		if (element is null || element.IsDeleted())
		{
			throw new ElementNotFoundException("The element does not exist or is deleted.");
		}
		return element;
	}

	protected abstract Task<ICollection<Account>> GetReferencingAccounts(Guid elementId, CancellationToken cancellationToken = default);

	protected abstract void ReplaceAccountReference(Account account, TElement destination);

	public async Task<ElementInfo> GetById(Guid id, CancellationToken cancellationToken = default)
	{
		TElement? element = await _repository.GetByIdAsync(id, cancellationToken);
		if (element is null || element.IsDeleted())
		{
			throw new ElementNotFoundException("The element does not exist or is deleted.");
		}
		TGroup? group = await _groupRepository.GetByIdAsync(element.GroupId, cancellationToken);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}
		return new ElementInfo
		{
			Id = element.Id,
			GroupId = element.GroupId,
			GroupName = group.Name,
			Name = element.Name,
			Description = element.Description,
			Order = element.Order,
			IsFavorite = element.IsFavorite
		};
	}
}
