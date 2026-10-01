using Business.Contracts.Base.Services;
using Business.Contracts.Params;
using Business.Contracts.Services.AccountGroups;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface IAccountGroupService : IGroupService<AccountGroup, Account, GroupParam>
{
	public Task<AccountsTreeInfo> GetAccountsTree();
}
