using Business.Models.Constants;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Utils.Models;

/// <summary>
/// Provides the predefined root groups used by the bookkeeping catalogs.
/// Each root is created once with a stable identifier and a self-parent relationship.
/// Consumers use these instances when initializing or navigating catalog hierarchies.
/// The identifier lookup methods recognize roots without depending on object identity.
/// The class does not persist roots or perform business workflow operations.
/// </summary>
public static class Roots
{
	public static AccountGroup AccountGroup { get; } = CreateAccountGroup();
	public static CategoryGroup CategoryGroup { get; } = CreateCategoryGroup();
	public static CorrespondentGroup CorrespondentGroup { get; } = CreateCorrespondentGroup();
	public static ProjectGroup ProjectGroup { get; } = CreateProjectGroup();
	public static TemplateGroup TemplateGroup { get; } = CreateTemplateGroup();

	private static HashSet<Guid> RootIds { get; } =
	new HashSet<Guid>
	{
		RootsIds.AccountGroupId,
		RootsIds.CategoryGroupId,
		RootsIds.CorrespondentGroupId,
		RootsIds.ProjectGroupId,
		RootsIds.TemplateGroupId
	};

	private static AccountGroup CreateAccountGroup()
	{
		AccountGroup group = new()
		{
			Id = RootsIds.AccountGroupId,
			ParentId = RootsIds.AccountGroupId,
			Parent = null!,
			Name = string.Empty
		};
		group.Parent = group;
		return group;
	}

	private static CategoryGroup CreateCategoryGroup()
	{
		CategoryGroup group = new()
		{
			Id = RootsIds.CategoryGroupId,
			ParentId = RootsIds.CategoryGroupId,
			Parent = null!,
			Name = string.Empty
		};
		group.Parent = group;
		return group;
	}

	private static CorrespondentGroup CreateCorrespondentGroup()
	{
		CorrespondentGroup group = new()
		{
			Id = RootsIds.CorrespondentGroupId,
			ParentId = RootsIds.CorrespondentGroupId,
			Parent = null!,
			Name = string.Empty
		};
		group.Parent = group;
		return group;
	}

	private static ProjectGroup CreateProjectGroup()
	{
		ProjectGroup group = new()
		{
			Id = RootsIds.ProjectGroupId,
			ParentId = RootsIds.ProjectGroupId,
			Parent = null!,
			Name = string.Empty
		};
		group.Parent = group;
		return group;
	}

	private static TemplateGroup CreateTemplateGroup()
	{
		TemplateGroup group = new()
		{
			Id = RootsIds.TemplateGroupId,
			ParentId = RootsIds.TemplateGroupId,
			Parent = null!,
			Name = string.Empty
		};
		group.Parent = group;
		return group;
	}

	public static bool IsRoot<TGroup, TElement>(TGroup group)
		where TGroup : class, IGroupEntity<TGroup, TElement>
		where TElement : class, IElementEntity<TGroup, TElement> =>
		RootIds.Contains(group.Id);

	public static bool IsRoot(Guid id) => RootIds.Contains(id);
}
