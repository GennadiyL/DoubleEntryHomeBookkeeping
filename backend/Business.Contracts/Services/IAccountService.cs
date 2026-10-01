using Business.Contracts.Base.Services;
using Business.Contracts.Params;
using Business.Models.Entities;

namespace Business.Contracts.Services;

public interface IAccountService : IElementService<AccountGroup, Account, AccountParam>
{
	/// <summary>
	/// Generates an account name for creation or restoring the name in the account editor.
	/// Uses current classification names and Local naming settings; absent slots retain separators.
	/// Returns the name without saving the account or calling AcceptChanges.
	/// </summary>
	public Task<string> GetDefaultName(Guid? correspondentId, Guid? categoryId, Guid? projectId);
}
