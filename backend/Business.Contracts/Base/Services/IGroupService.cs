using Business.Contracts.Services.Groups;
using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

public interface IGroupService<TGroup, TElement, in TParam> : ICatalogService<TGroup, TParam>
	where TGroup : class, IGroupEntity<TGroup, TElement>, ICatalogEntity
	where TElement : class, IElementEntity<TGroup, TElement>, ICatalogEntity
	where TParam : class
{
	/// <summary>
	/// Returns all groups of this catalog, including the root once, for building the selection tree.
	/// The flat result includes ParentId and Order; this read does not save changes.
	/// </summary>
	public Task<List<GroupInfo>> GetAllGroups();

	/// <summary>
	/// Returns all category groups and their elements for browsing and selection.
	/// Groups and elements are separate flat collections connected by GroupId; no changes are saved.
	/// </summary>
	public Task<TreeInfo> GetTree();

	public Task MoveToAnotherParent(Guid groupId, Guid toParentId);
	public Task CombineGroups(Guid toGroupId, Guid fromGroupId);
}
