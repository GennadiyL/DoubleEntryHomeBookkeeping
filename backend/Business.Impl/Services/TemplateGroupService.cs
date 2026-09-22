using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class TemplateGroupService : GroupService<TemplateGroup, Template>, ITemplateGroupService
{
	public TemplateGroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.TemplateGroupRepo, unitOfWork.TemplateRepo)
	{
	}
}
