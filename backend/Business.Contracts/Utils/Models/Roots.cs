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
	public static readonly Guid AccountGroupId = new("22D0BBCC-37EC-4AF4-B5C7-9CAF34FEC1E9");
	public static readonly Guid CategoryGroupId = new("CDB033F6-8686-4222-B33F-66B5A3BF2948");
	public static readonly Guid CorrespondentGroupId = new("9C19A1FE-9C57-4703-9105-79075987EE45");
	public static readonly Guid ProjectGroupId = new("7C9385F6-28F2-4947-8B44-E81DD8D01949");
	public static readonly Guid TemplateGroupId = new("B232A84F-47D8-426E-AA54-BA8771B8B6DE");

	public static AccountGroup AccountGroup { get; } = CreateAccountGroup();
	public static CategoryGroup CategoryGroup { get; } = CreateCategoryGroup();
	public static CorrespondentGroup CorrespondentGroup { get; } = CreateCorrespondentGroup();
	public static ProjectGroup ProjectGroup { get; } = CreateProjectGroup();
	public static TemplateGroup TemplateGroup { get; } = CreateTemplateGroup();

	private static HashSet<Guid> RootIds { get; } =
	[
		AccountGroupId,
		CategoryGroupId,
		CorrespondentGroupId,
		ProjectGroupId,
		TemplateGroupId
	];

	private static AccountGroup CreateAccountGroup()
	{
		AccountGroup group = new()
		{
			Id = AccountGroupId,
			ParentId = AccountGroupId,
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
			Id = CategoryGroupId,
			ParentId = CategoryGroupId,
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
			Id = CorrespondentGroupId,
			ParentId = CorrespondentGroupId,
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
			Id = ProjectGroupId,
			ParentId = ProjectGroupId,
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
			Id = TemplateGroupId,
			ParentId = TemplateGroupId,
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
