using Business.Contracts.Services.Trees;

namespace Business.Contracts.Services.Accounts;

public record AccountTreeInfo
{
	public List<GroupInfo> Groups { get; } = new();
	public List<AccountElementInfo> Elements { get; } = new();
}
