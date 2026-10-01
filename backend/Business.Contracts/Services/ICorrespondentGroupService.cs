using Business.Contracts.Base.Services;
using Business.Contracts.Params;
using Business.Contracts.Services.Groups;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICorrespondentGroupService : IGroupService<CorrespondentGroup, Correspondent>, IUpdateEntityService<GroupParam>
{
}
