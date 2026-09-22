using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class CorrespondentGroupService : GroupService<CorrespondentGroup, Correspondent>, ICorrespondentGroupService
{
	public CorrespondentGroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.CorrespondentGroupRepo, unitOfWork.CorrespondentRepo)
	{
	}
}
