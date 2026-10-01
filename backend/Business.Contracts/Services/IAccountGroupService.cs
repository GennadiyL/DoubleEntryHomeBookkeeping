using Business.Contracts.Base.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

/// <summary>
/// Provides Account group reads and maintenance for its catalog hierarchy.
/// Composes group editor operations, full group reads, ordering and favorite selection.
/// Uses the shared GroupParam and GroupInfo contracts for non-root editing.
/// The fixed root is included in reads and protected from mutation.
/// GetAccountsTree additionally supplies currency labels for the account tree.
/// State-changing actions save all affected rows and tracking atomically.
/// </summary>
public interface IAccountGroupService :
	IGroupService<AccountGroup, Account>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
	/// <summary>
	/// Returns the live account hierarchy for browsing and account selection.
	/// Includes the root once and direct root accounts in separate flat group and element collections.
	/// Account rows add CurrencyName to the common Name, Description and IsFavorite columns.
	/// Full classification values belong to the separate account edit read; this operation saves nothing.
	/// </summary>
	public Task<AccountTreeInfo> GetAccountsTree(CancellationToken cancellationToken = default);
}
