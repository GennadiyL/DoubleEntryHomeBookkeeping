using Business.Contracts.Base.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface IAccountGroupService :
	IGroupService<AccountGroup, Account>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
	public Task<AccountTreeInfo> GetAccountsTree();
}
