using Business.Contracts.Utils.Merging;
using Business.Contracts.Utils.Models;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using Business.Models.Enums;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using Shared.Contracts;
using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;

namespace Business.Impl.Services.Base;

public abstract class GroupService<TGroup, TElement> : IGroupService<TGroup, TElement>, IUpdateEntityService<GroupParam>, IReadEntityService<GroupInfo>
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
		if (parent is null || parent.IsDeleted())
		{
			throw new InvalidGroupException("The parent group does not exist or is deleted.");
		}

		string name = param.Name.Trim();
		List<TGroup> siblings = [.. parent.Children.Where(child => child.Id != parent.Id)];
		if (siblings.Any(child => string.Equals(child.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidGroupException("A group with the same name already exists in this parent.");
		}

		int maxOrder = siblings.Count == 0 ? 0 : siblings.Max(child => child.Order);
		if (maxOrder == int.MaxValue)
		{
			throw new InvalidGroupException("The parent group has no available ordering position.");
		}

		TGroup group = new()
		{
			Id = Guid.NewGuid(),
			Name = name,
			Description = param.Description,
			IsFavorite = param.IsFavorite,
			ParentId = parent.Id,
			Parent = parent,
			Order = maxOrder + 1,
			EditRevision = null,
			DeleteRevision = null,
			ModificationType = ModificationType.None,
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
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (Roots.IsRoot(group.Id))
		{
			throw new InvalidGroupException("The root group cannot be edited.");
		}

		if (param.ParentId != group.ParentId)
		{
			throw new InvalidGroupException("Use MoveToAnotherParent to change the parent group.");
		}

		TGroup? parent = await _repository.GetWithChildrenByIdAsync(group.ParentId);
		if (parent is null || parent.IsDeleted())
		{
			throw new InvalidGroupException("The parent group does not exist or is deleted.");
		}

		string name = param.Name.Trim();
		if (parent.Children.Any(child =>
			child.Id != group.Id && child.Id != parent.Id &&
			string.Equals(child.Name.Trim(), name, StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidGroupException("A group with the same name already exists in this parent.");
		}

		group.Name = name;
		group.Description = param.Description;
		group.IsFavorite = param.IsFavorite;
		group.SetEditedContent();
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
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (Roots.IsRoot(group.Id) || group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be deleted.");
		}

		if (group.Children.Any(child => !child.IsDeleted()) ||
			group.Elements.Any(element => !element.IsDeleted()))
		{
			throw new InvalidGroupException("A group with active child groups or elements cannot be deleted.");
		}

		TGroup? parent = await _repository.GetWithChildrenByIdAsync(group.ParentId);
		if (parent is null || parent.IsDeleted())
		{
			throw new InvalidGroupException("The parent group does not exist or is deleted.");
		}

		List<TGroup> survivors = [.. parent.Children
			.Where(child => child.Id != parent.Id && child.Id != group.Id && !child.IsDeleted())
			.OrderBy(child => child.Order)
			.ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];

		group.SetDeleted();
		group.SetEditedContent();
		_repository.Update(group);

		for (int order = 0; order < survivors.Count; order++)
		{
			TGroup sibling = survivors[order];
			if (sibling.Order == order)
			{
				continue;
			}
			sibling.Order = order;
			sibling.SetEditedOrder();
			_repository.Update(sibling);
		}
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task SetOrder(Guid entityId, int order)
	{
		if (entityId == Guid.Empty || order < 0)
		{
			throw new InvalidGroupException("A group identifier and a nonnegative order are required.");
		}

		TGroup? group = await _repository.GetByIdAsync(entityId, CancellationToken.None);
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (Roots.IsRoot(group.Id) || group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be reordered.");
		}

		TGroup? parent = await _repository.GetWithChildrenByIdAsync(group.ParentId);
		if (parent is null || parent.IsDeleted())
		{
			throw new GroupNotFoundException("The parent group does not exist or is deleted.");
		}

		List<TGroup> siblings = [.. parent.Children
			.Where(child => child.Id != parent.Id && !child.IsDeleted())
			.OrderBy(child => child.Order).ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];
		TGroup? target = siblings.SingleOrDefault(child => child.Id == entityId);
		if (target is null)
		{
			throw new GroupNotFoundException("The group is no longer an active child of this parent.");
		}

		if (order >= siblings.Count)
		{
			throw new InvalidGroupException("The order exceeds the number of active sibling groups.");
		}

		Dictionary<Guid, int> originalOrders = siblings.ToDictionary(child => child.Id, child => child.Order);
		siblings.Remove(target);
		siblings.Insert(order, target);
		for (int position = 0; position < siblings.Count; position++)
		{
			siblings[position].Order = position;
		}
		List<TGroup> changed = [.. siblings.Where(child => child.Order != originalOrders[child.Id])];
		if (changed.Count == 0)
		{
			return;
		}

		foreach (TGroup sibling in changed)
		{
			sibling.SetEditedOrder();
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
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (Roots.IsRoot(group.Id) || group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be marked or unmarked as a favorite.");
		}

		if (group.IsFavorite == isFavorite)
		{
			return;
		}

		group.IsFavorite = isFavorite;
		group.SetEditedContent();
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
		if (group is null || group.IsDeleted())
		{
			throw new GroupNotFoundException("The group does not exist or is deleted.");
		}

		if (Roots.IsRoot(group.Id) || group.ParentId == group.Id)
		{
			throw new InvalidGroupException("The root group cannot be moved.");
		}

		if (groupId == toParentId)
		{
			throw new InvalidGroupException("A group cannot be moved into itself.");
		}

		TGroup? destination = await _repository.GetWithChildrenByIdAsync(toParentId);
		if (destination is null || destination.IsDeleted())
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
			if (next is null || next.IsDeleted())
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
		if (siblings.Any(child => string.Equals(child.Name.Trim(), group.Name.Trim(), StringComparison.OrdinalIgnoreCase)))
		{
			throw new InvalidGroupException("A group with the same name already exists in the destination parent.");
		}

		TGroup? source = await _repository.GetWithChildrenByIdAsync(group.ParentId);
		if (source is null || source.IsDeleted())
		{
			throw new GroupNotFoundException("The source parent does not exist or is deleted.");
		}

		List<TGroup> sourceSiblings = [.. source.Children
			.Where(child => child.Id != source.Id && child.Id != group.Id && !child.IsDeleted())
			.OrderBy(child => child.Order).ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];
		List<TGroup> destinationSiblings = [.. siblings.Where(child => !child.IsDeleted())
			.OrderBy(child => child.Order).ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];

		group.ParentId = destination.Id;
		group.Parent = destination;
		group.Order = destinationSiblings.Count;
		group.SetEditedContent();
		group.SetEditedOrder();
		_repository.Update(group);

		foreach (List<TGroup> collection in new[] { sourceSiblings, destinationSiblings })
		{
			for (int order = 0; order < collection.Count; order++)
			{
				TGroup sibling = collection[order];
				if (sibling.Order == order)
				{
					continue;
				}
				sibling.Order = order;
				sibling.SetEditedOrder();
				_repository.Update(sibling);
			}
		}
		await _unitOfWork.SaveChangesAsync();
	}

	public async Task CombineGroups(Guid toGroupId, Guid fromGroupId)
	{
		if (toGroupId == fromGroupId)
		{
			return;
		}

		if (toGroupId == Guid.Empty || fromGroupId == Guid.Empty)
		{
			throw new InvalidGroupException("Two different group identifiers are required.");
		}

		TGroup? source = await _repository.GetWithContentsByIdAsync(fromGroupId);
		if (source is null || source.IsDeleted())
		{
			throw new GroupNotFoundException("The source group does not exist or is deleted.");
		}

		if (Roots.IsRoot(source.Id) || source.ParentId == source.Id)
		{
			throw new InvalidGroupException("The root group cannot be combined into another group.");
		}

		TGroup? destination = await _repository.GetWithContentsByIdAsync(toGroupId);
		if (destination is null || destination.IsDeleted())
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
			if (next is null || next.IsDeleted())
			{
				throw new GroupNotFoundException("A destination ancestor does not exist or is deleted.");
			}
			ancestor = next;
		}

		TGroup? parent = source.ParentId == destination.Id
			? destination
			: await _repository.GetWithChildrenByIdAsync(source.ParentId);
		if (parent is null || parent.IsDeleted())
		{
			throw new GroupNotFoundException("The source parent does not exist or is deleted.");
		}

		List<TGroup> children = [.. source.Children.OrderBy(child => child.Order)
			.ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];
		List<TElement> elements = [.. source.Elements.OrderBy(element => element.Order)
			.ThenBy(element => element.Id.ToString("D"), StringComparer.Ordinal)];
		List<TGroup> destinationChildren = [.. destination.Children.Where(child => child.Id != destination.Id)];
		List<TGroup> liveDestinationChildren = [.. destinationChildren
			.Where(child => child.Id != source.Id && !child.IsDeleted())
			.OrderBy(child => child.Order).ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];
		List<TElement> liveDestinationElements = [.. destination.Elements.Where(element => !element.IsDeleted())
			.OrderBy(element => element.Order).ThenBy(element => element.Id.ToString("D"), StringComparer.Ordinal)];
		List<TGroup> sourceSiblings = parent.Id == destination.Id ? [] : [.. parent.Children
			.Where(child => child.Id != parent.Id && child.Id != source.Id && !child.IsDeleted())
			.OrderBy(child => child.Order).ThenBy(child => child.Id.ToString("D"), StringComparer.Ordinal)];

		HashSet<string> groupNames = new(destinationChildren.Select(child => child.Name.Trim()), StringComparer.OrdinalIgnoreCase);
		HashSet<string> elementNames = new(destination.Elements.Select(element => element.Name.Trim()), StringComparer.OrdinalIgnoreCase);
		int groupOrder = liveDestinationChildren.Count;
		int elementOrder = liveDestinationElements.Count;
		foreach (TGroup child in children)
		{
			child.Name = GetUniqueCombinedName(child.Name.Trim(), groupNames);
			child.ParentId = destination.Id;
			child.Parent = destination;
			if (!child.IsDeleted())
			{
				child.Order = groupOrder++;
			}
			child.SetEditedContent();
			child.SetEditedOrder();
			_repository.Update(child);
			destination.Children.Add(child);
		}
		foreach (TElement element in elements)
		{
			if (typeof(TElement) != typeof(Business.Models.Entities.Account))
			{
				element.Name = GetUniqueCombinedName(element.Name.Trim(), elementNames);
			}
			element.GroupId = destination.Id;
			element.Group = destination;
			if (!element.IsDeleted())
			{
				element.Order = elementOrder++;
			}
			element.SetEditedContent();
			element.SetEditedOrder();
			_elementRepository.Update(element);
			destination.Elements.Add(element);
		}

		foreach (List<TGroup> collection in new[] { sourceSiblings, liveDestinationChildren })
		{
			for (int order = 0; order < collection.Count; order++)
			{
				TGroup sibling = collection[order];
				if (sibling.Order == order) { continue; }
				sibling.Order = order;
				sibling.SetEditedOrder();
				_repository.Update(sibling);
			}
		}
		for (int order = 0; order < liveDestinationElements.Count; order++)
		{
			TElement element = liveDestinationElements[order];
			if (element.Order == order) { continue; }
			element.Order = order;
			element.SetEditedOrder();
			_elementRepository.Update(element);
		}

		source.Children.Clear();
		source.Elements.Clear();
		source.SetDeleted();
		source.SetEditedContent();
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

	public Task<List<GroupInfo>> GetAllGroups() => throw new NotImplementedException();

	public Task<TreeInfo> GetTree() => throw new NotImplementedException();

	public Task<GroupInfo> GetById(Guid id) => throw new NotImplementedException();
}
