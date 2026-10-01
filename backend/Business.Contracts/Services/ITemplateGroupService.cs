using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ITemplateGroupService :
	IGroupService<TemplateGroup, Template>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
}
