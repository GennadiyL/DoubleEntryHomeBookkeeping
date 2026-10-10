using Dehb.WinUi.Hosting;
using Business.Contracts.Base.Services;
using Business.Contracts.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;
using Business.Models.Entities.Interfaces;

namespace Dehb.WinUi.Catalogs;

internal sealed class CatalogSource
{
	public required string Name { get; init; }
	public required Func<TreeInfo> Read { get; init; }
	public required Func<Guid?, GroupParam, Guid> SaveGroup { get; init; }
	public required Action<bool, Guid> Delete { get; init; }
	public required Action<bool, Guid, bool> Favorite { get; init; }
	public required Action<bool, Guid, Guid> Move { get; init; }
	public required Action<bool, Guid, Guid> Merge { get; init; }
	public required Action<bool, Guid, int> Order { get; init; }
	public required Func<Guid?, ElementParam, Guid> SaveElement { get; init; }
	public static CatalogSource For(BusinessHost host, string name) => name switch
	{
		"Accounts" => Create<AccountGroup, Account, IAccountGroupService, IAccountService>(host, name),
		"Categories" => Create<CategoryGroup, Category, ICategoryGroupService, ICategoryService>(host, name),
		"Correspondents" => Create<CorrespondentGroup, Correspondent, ICorrespondentGroupService, ICorrespondentService>(host, name),
		"Projects" => Create<ProjectGroup, Project, IProjectGroupService, IProjectService>(host, name),
		"Templates" => Create<TemplateGroup, Template, ITemplateGroupService, ITemplateService>(host, name),
		_ => throw new ArgumentException("Unknown catalog.")
	};
	private static CatalogSource Create<TG, TE, TGs, TEs>(BusinessHost host, string name)
		where TG : class, IGroupEntity<TG, TE>, ICatalogEntity
		where TE : class, IElementEntity<TG, TE>, ICatalogEntity
		where TGs : class, IGroupService<TG, TE>, IUpdateEntityService<GroupParam>
		where TEs : class, IElementService<TG, TE>
	{
		return new CatalogSource
		{
			Name = name,
			Read = () => host.Call<TGs, TreeInfo>(s => s.GetTree()),
			SaveGroup = (id, p) => id is null ? host.Call<TGs, Guid>(s => s.Add(p)) : host.Call<TGs, Guid>(async s => { await s.Update(id.Value, p); return id.Value; }),
			SaveElement = (id, p) => host.Call<TEs, Guid>(async s =>
			{
				IUpdateEntityService<ElementParam> service = (IUpdateEntityService<ElementParam>)s;
				if (id is null)
				{
					return await service.Add(p);
				}

				await service.Update(id.Value, p); return id.Value;
			}),
			Delete = (group, id) =>
			{
				if (group)
				{
					host.Call<TGs>(s => s.Delete(id));
				}
				else if (name == "Accounts")
				{
					host.Call<IAccountService>(s => s.Delete(id));
				}
				else if (name == "Templates")
				{
					host.Call<ITemplateService>(s => s.Delete(id));
				}
				else
				{
					host.Call<TEs>(s => ((IUpdateEntityService<ElementParam>)s).Delete(id));
				}
			},
			Favorite = (group, id, value) => {
				if (group)
				{
					host.Call<TGs>(s => s.SetFavoriteStatus(id, value));
				}
				else
				{
					host.Call<TEs>(s => s.SetFavoriteStatus(id, value));
				}
			},
			Move = (group, id, to) => {
				if (group)
				{
					host.Call<TGs>(s => s.MoveToAnotherParent(id, to));
				}
				else
				{
					host.Call<TEs>(s => s.MoveToAnotherGroup(id, to));
				}
			},
			Merge = (group, id, to) => {
				if (group)
				{
					host.Call<TGs>(s => s.CombineGroups(to, id));
				}
				else
				{
					host.Call<TEs>(s => s.CombineElements(to, id));
				}
			},
			Order = (group, id, position) =>
			{
				if (group)
				{
					host.Call<TGs>(s => s.SetOrder(id, position));
				}
				else
				{
					host.Call<TEs>(s => s.SetOrder(id, position));
				}
			}
		};
	}
}
