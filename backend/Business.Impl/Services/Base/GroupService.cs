using Business.Contracts.Params;
using Business.Contracts.Services.Base;
using Business.Contracts.Utils.Ordering;
using Business.Models.Entities.Base;
using Business.Models.Entities.Interfaces;
using Business.Models.Exceptions;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories.Base;
using Shared.Contracts;

namespace Business.Impl.Services.Base;

public abstract class GroupService<TGroup, TElement> : IGroupService<TGroup, TElement, GroupParam>
	where TGroup : GroupEntity<TGroup, TElement>, new()
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
{
	private readonly ISharedContext _sharedContext;
	private readonly IAppUnitOfWork _unitOfWork;
	private readonly IGroupRepository<TGroup, TElement> _repository;

	protected GroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork,
		IGroupRepository<TGroup, TElement> repository)
	{
		_sharedContext = sharedContext;
		_unitOfWork = unitOfWork;
		_repository = repository;
	}

	public async Task<Guid> Add(GroupParam param)
	{
		if (param is null || string.IsNullOrWhiteSpace(param.Name) || param.ParentId == Guid.Empty)
		{
			throw new InvalidGroupException("A group name and parent identifier are required.");
		}

		TGroup? parent = await _repository.GetParentWithChildrenByParentId(param.ParentId);
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

		TGroup? parent = await _repository.GetParentWithChildrenByParentId(group.ParentId);
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

		TGroup? parent = await _repository.GetParentWithChildrenByParentId(group.ParentId);
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
	public Task SetFavoriteStatus(Guid entityId, bool isFavorite) => throw new NotImplementedException();
	public Task MoveToAnotherParent(Guid groupId, Guid toParentId) => throw new NotImplementedException();
	public Task CombineGroups(Guid toGroupId, Guid fromGroupId) => throw new NotImplementedException();
}
