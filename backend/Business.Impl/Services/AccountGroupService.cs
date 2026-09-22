using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class AccountGroupService : GroupService<AccountGroup, Account>, IAccountGroupService
{
	public AccountGroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.AccountGroupRepo, unitOfWork.AccountRepo)
	{
	}
}
