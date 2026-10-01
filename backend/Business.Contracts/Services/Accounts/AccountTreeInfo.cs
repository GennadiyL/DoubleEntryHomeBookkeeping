using Business.Contracts.Services.Trees;

namespace Business.Contracts.Services.Accounts;

/// <summary>
/// Contains the live account hierarchy for browsing and account selection.
/// Groups uses the shared group projection and includes the root exactly once.
/// Elements includes accounts assigned directly to the root or any other group.
/// ParentId and GroupId connect the two separate flat collections.
/// Each collection retains a stable list instance and uses catalog ordering.
/// Account rows add CurrencyName to Name, Description and IsFavorite.
/// Account classifications are available only through the full account edit read.
/// This detached read result exposes no synchronization state and saves nothing.
/// </summary>
public record AccountTreeInfo
{
	public List<GroupInfo> Groups { get; } = new();
	public List<AccountElementInfo> Elements { get; } = new();
}
