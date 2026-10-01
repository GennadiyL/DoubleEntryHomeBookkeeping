using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICorrespondentGroupService :
	IGroupService<CorrespondentGroup, Correspondent>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
}
