using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class CategoryGroupService : GroupService<CategoryGroup, Category>, ICategoryGroupService
{
	public CategoryGroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.CategoryGroupRepo)
	{
	}
}
