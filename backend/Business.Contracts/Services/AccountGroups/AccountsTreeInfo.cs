using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Groups;

namespace Business.Contracts.Services.AccountGroups;

public record AccountsTreeInfo
{
	public List<GroupInfo> Groups { get; } = new();
	public List<AccountInfo> Elements { get; } = new();
}
