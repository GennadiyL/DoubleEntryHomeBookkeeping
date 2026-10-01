using Business.Contracts.Params;
using Business.Contracts.Services.Base;
using Business.Contracts.Services.Groups;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface ICorrespondentGroupService : IGroupService<CorrespondentGroup, Correspondent, GroupParam>
{
}
