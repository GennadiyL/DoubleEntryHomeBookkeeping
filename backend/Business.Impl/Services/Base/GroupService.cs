using Business.Contracts.Params;
using Business.Contracts.Services.Base;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using Shared.Contracts;

namespace Business.Impl.Services.Base;

public abstract class GroupService<TGroup, TElement> : IGroupService<TGroup, TElement, GroupParam>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
{
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IGroupRepository<TGroup, TElement> _repository;
	private readonly IRepository<TElement> _elementRepository;

	protected GroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork,
		IGroupRepository<TGroup, TElement> repository, IRepository<TElement> elementRepository)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_repository = repository;
		_elementRepository = elementRepository;
	}

	public async Task<Guid> Add(GroupParam param)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.ParentId == Guid.Empty)
		{
			throw new InvalidGroupException("A group name and parent identifier are required.");
		}

		TGroup? parent = await _repository.GetWithChildrenByIdAsync(param.ParentId);
		if (parent is null || parent.IsDeleted)
		{
			throw new GroupNotFoundException("The parent group does not exist or is deleted.");
		}

		List<TGroup> siblings = [.. parent.Children.Where(child => child.Id != parent.Id)];
		if (siblings.Any(child => string.Equals(child.Name, param.Name, StringComparison.Ordinal)))
		{
			throw new InvalidGroupException("A group with the same name already exists in this parent.");
		}

		int maxOrder = siblings.Count == 0 ? 0 : siblings.Max(child => child.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidGroupException("The parent group has no available ordering position.");
		}

		DateTime now = _sharedContext.DateTimeService.UtcNow;
		TGroup group = new()
		{
			Id = Guid.NewGuid(),
			Name = param.Name,
			Description = param.Description,
			IsFavorite = param.IsFavorite,
			ParentId = parent.Id,
			Parent = parent,
			Order = maxOrder + 1,
			Original = now,
			Current = now
		};
		_repository.Add(group);
		await _unitOfWork.SaveChangesAsync();
		return group.Id;
	}

	public async Task Update(Guid entityId, GroupParam param)
	{
		if (entityId == Guid.Empty || param is null ||
			string.IsNullOrWhiteSpace(param.Name) || param.ParentId == Guid.Empty)
		{
			throw new InvalidGroupException("A group identifier, name, and parent identifier are required.");
		}

		TGroup? group = await _repository.GetByIdAsync(entityId, CancellationToken.None);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (param.ParentId != group.ParentId)
		{
			throw new InvalidGroupException("Use MoveToAnotherParent to change the parent group.");
		}

		TGroup? parent = await _repository.GetWithChildrenByIdAsync(group.ParentId);
		if (parent is null || parent.IsDeleted)
		{
			throw new GroupNotFoundException("The parent group does not exist or is deleted.");
		}

		if (group.Id != parent.Id && parent.Children.Any(child =>
			child.Id != group.Id && child.Id != parent.Id &&
			string.Equals(child.Name, param.Name, StringComparison.Ordinal)))
		{
			throw new InvalidGroupException("A group with the same name already exists in this parent.");
		}

		group.Name = param.Name;
		group.Description = param.Description;
		group.IsFavorite = param.IsFavorite;
		group.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(group);
		await _unitOfWork.SaveChangesAsync();
	}
	public async Task Delete(Guid entityId)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidGroupException("A group identifier is required.");
		}

		TGroup? group = await _repository.GetWithContentsByIdAsync(entityId);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be deleted.");
		}

		if (group.Children.Any(child => !child.IsDeleted) ||
			group.Elements.Any(element => !element.IsDeleted))
		{
			throw new InvalidGroupException("A group with active child groups or elements cannot be deleted.");
		}

		group.IsDeleted = true;
		group.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(group);
		await _unitOfWork.SaveChangesAsync();
	}
	public async Task SetOrder(Guid entityId, int order)
	{
		if (entityId == Guid.Empty || order < 1)
		{
			throw new InvalidGroupException("A group identifier and a positive order are required.");
		}

		TGroup? group = await _repository.GetByIdAsync(entityId, CancellationToken.None);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be reordered.");
		}

		TGroup? parent = await _repository.GetWithChildrenByIdAsync(group.ParentId);
		if (parent is null || parent.IsDeleted)
		{
			throw new GroupNotFoundException("The parent group does not exist or is deleted.");
		}

		List<TGroup> siblings = [.. parent.Children
			.Where(child => child.Id != parent.Id && !child.IsDeleted)
			.OrderBy(child => child.Order).ThenBy(child => child.Id)];
		TGroup? target = siblings.SingleOrDefault(child => child.Id == entityId);
		if (target is null)
		{
			throw new GroupNotFoundException("The group is no longer an active child of this parent.");
		}

		if (order > siblings.Count)
		{
			throw new InvalidGroupException("The order exceeds the number of active sibling groups.");
		}

		Dictionary<Guid, int> originalOrders = siblings.ToDictionary(child => child.Id, child => child.Order);
		siblings.Reorder();
		siblings.SetOrder(target, order);
		List<TGroup> changed = [.. siblings.Where(child => child.Order != originalOrders[child.Id])];
		if (changed.Count == 0)
		{
			return;
		}

		DateTime now = _sharedContext.DateTimeService.UtcNow;
		foreach (TGroup sibling in changed)
		{
			sibling.Current = now;
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChangesAsync();
	}
	public async Task SetFavoriteStatus(Guid entityId, bool isFavorite)
	{
		if (entityId == Guid.Empty)
		{
			throw new InvalidGroupException("A group identifier is required.");
		}

		TGroup? group = await _repository.GetByIdAsync(entityId, CancellationToken.None);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (group.IsFavorite == isFavorite)
		{
			return;
		}

		group.IsFavorite = isFavorite;
		group.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(group);
		await _unitOfWork.SaveChangesAsync();
	}
	public async Task MoveToAnotherParent(Guid groupId, Guid toParentId)
	{
		if (groupId == Guid.Empty || toParentId == Guid.Empty)
		{
			throw new InvalidGroupException("A group identifier and destination parent identifier are required.");
		}

		TGroup? group = await _repository.GetByIdAsync(groupId, CancellationToken.None);
		if (group is null || group.IsDeleted)
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be moved.");
		}

		if (groupId == toParentId)
		{
			throw new InvalidGroupException("A group cannot be moved into itself.");
		}

		TGroup? destination = await _repository.GetWithChildrenByIdAsync(toParentId);
		if (destination is null || destination.IsDeleted)
		{
			throw new GroupNotFoundException("The destination parent does not exist or is deleted.");
		}

		HashSet<Guid> visited = [];
		TGroup ancestor = destination;
		while (true)
		{
			if (ancestor.Id == groupId || !visited.Add(ancestor.Id))
			{
				throw new InvalidGroupException("The move would create or enter a group hierarchy cycle.");
			}

			if (ancestor.ParentId == ancestor.Id)
			{
				break;
			}

			TGroup? next = await _repository.GetByIdAsync(ancestor.ParentId, CancellationToken.None);
			if (next is null || next.IsDeleted)
			{
				throw new GroupNotFoundException("A destination ancestor does not exist or is deleted.");
			}
			ancestor = next;
		}

		if (group.ParentId == toParentId)
		{
			return;
		}

		List<TGroup> siblings = [.. destination.Children.Where(child => child.Id != destination.Id)];
		if (siblings.Any(child => string.Equals(child.Name, group.Name, StringComparison.Ordinal)))
		{
			throw new InvalidGroupException("A group with the same name already exists in the destination parent.");
		}

		int maxOrder = siblings.Count == 0 ? 0 : siblings.Max(child => child.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidGroupException("The destination parent has no available ordering position.");
		}

		group.ParentId = destination.Id;
		group.Parent = destination;
		group.Order = maxOrder + 1;
		group.Current = _sharedContext.DateTimeService.UtcNow;
		_repository.Update(group);
		await _unitOfWork.SaveChangesAsync();
	}
	public async Task CombineGroups(Guid toGroupId, Guid fromGroupId)
	{
		if (toGroupId == Guid.Empty || fromGroupId == Guid.Empty || toGroupId == fromGroupId)
		{
			throw new InvalidGroupException("Two different group identifiers are required.");
		}

		TGroup? source = await _repository.GetWithContentsByIdAsync(fromGroupId);
		if (source is null || source.IsDeleted)
		{
			throw new GroupNotFoundException("The source group does not exist or is deleted.");
		}

		if (source.ParentId == source.Id)
		{
			throw new InvalidGroupException("The root group cannot be combined into another group.");
		}

		TGroup? destination = await _repository.GetWithContentsByIdAsync(toGroupId);
		if (destination is null || destination.IsDeleted)
		{
			throw new GroupNotFoundException("The destination group does not exist or is deleted.");
		}

		HashSet<Guid> visited = [];
		TGroup ancestor = destination;
		while (true)
		{
			if (ancestor.Id == fromGroupId || !visited.Add(ancestor.Id))
			{
				throw new InvalidGroupException("Combining these groups would create or enter a hierarchy cycle.");
			}
			if (ancestor.ParentId == ancestor.Id)
			{
				break;
			}
			TGroup? next = await _repository.GetByIdAsync(ancestor.ParentId, CancellationToken.None);
			if (next is null || next.IsDeleted)
			{
				throw new GroupNotFoundException("A destination ancestor does not exist or is deleted.");
			}
			ancestor = next;
		}

		List<TGroup> children = [.. source.Children.OrderBy(child => child.Order).ThenBy(child => child.Id)];
		List<TElement> elements = [.. source.Elements.OrderBy(element => element.Order).ThenBy(element => element.Id)];
		List<TGroup> destinationChildren = [.. destination.Children.Where(child => child.Id != destination.Id)];
		int groupOrder = Math.Max(0, destinationChildren.GetMaxOrder());
		int elementOrder = Math.Max(0, destination.Elements.GetMaxOrder());
		if ((long)groupOrder + children.Count > int.MaxValue ||
			(long)elementOrder + elements.Count > int.MaxValue)
		{
			throw new InvalidGroupException("The destination has no available ordering positions.");
		}

		HashSet<string> groupNames = new(destinationChildren.Select(child => child.Name), StringComparer.Ordinal);
		HashSet<string> elementNames = new(destination.Elements.Select(element => element.Name), StringComparer.Ordinal);
		DateTime now = _sharedContext.DateTimeService.UtcNow;
		foreach (TGroup child in children)
		{
			child.Name = GetUniqueCombinedName(child.Name, groupNames);
			child.ParentId = destination.Id;
			child.Parent = destination;
			child.Order = ++groupOrder;
			child.Current = now;
			_repository.Update(child);
			destination.Children.Add(child);
		}
		foreach (TElement element in elements)
		{
			element.Name = GetUniqueCombinedName(element.Name, elementNames);
			element.GroupId = destination.Id;
			element.Group = destination;
			element.Order = ++elementOrder;
			element.Current = now;
			_elementRepository.Update(element);
			destination.Elements.Add(element);
		}

		source.Children.Clear();
		source.Elements.Clear();
		source.IsDeleted = true;
		source.Current = now;
		_repository.Update(source);
		await _unitOfWork.SaveChangesAsync();
	}

	private static string GetUniqueCombinedName(string name, HashSet<string> names)
	{
		while (!names.Add(name))
		{
			name += "_1";
		}
		return name;
	}
}
