using Business.Contracts.Services.Accounts;
using Business.Contracts.Base.Services;
using Business.Models.Entities;

namespace Business.Contracts.Services;

/// <summary>
/// Provides account reads, editing, movement and same-currency replacement.
/// Full editor reads include currency and optional classification labels.
/// The first save fixes account currency; duplicate account names are permitted.
/// Transaction and template references prevent ordinary account deletion.
/// Replacement rewrites those references and marks each affected parent aggregate as content-edited.
/// Default-name generation is a preview using Local naming settings and saves nothing.
/// </summary>
public interface IAccountService :
	IElementService<AccountGroup, Account>,
	IUpdateEntityService<AccountParam>,
	IReadEntityService<AccountInfo>
{
	/// <summary>
	/// Generates an account name for creation or restoring the name in the account editor.
	/// Uses current classification names and Local naming settings; absent slots retain separators.
	/// Returns the name without saving the account or calling AcceptChanges.
	/// </summary>
	public Task<string> GetDefaultName(Guid? correspondentId, Guid? categoryId, Guid? projectId, CancellationToken cancellationToken = default);
}
