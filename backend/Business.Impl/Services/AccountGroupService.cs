using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Impl.Services.Base;
using Business.Contracts.Utils.Merging;
using Business.Contracts.Services.Trees;
using Business.Models.Exceptions;
using Business.Models.Entities;
using DataAccess.Contracts;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class AccountGroupService : GroupService<AccountGroup, Account>, IAccountGroupService
{
	private readonly IAppUnitOfWork _unitOfWork;

	public AccountGroupService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.AccountGroupRepo, unitOfWork.AccountRepo)
	{
		_unitOfWork = unitOfWork;
	}

	public async Task<AccountTreeInfo> GetAccountsTree(CancellationToken cancellationToken = default)
	{
		AccountTreeInfo result = new();
		result.Groups.AddRange(await GetAllGroups(cancellationToken));
		Dictionary<Guid, GroupInfo> groups = result.Groups.ToDictionary(group => group.Id);
		ICollection<Account> accounts = await _unitOfWork.AccountRepo.GetAll(cancellationToken);
		ICollection<Currency> currencies = await _unitOfWork.CurrencyRepo.GetAll(cancellationToken);
		Dictionary<Guid, Currency> activeCurrencies = currencies.Where(currency => !currency.IsDeleted()).ToDictionary(currency => currency.Id);
		foreach (Account account in accounts.Where(account => !account.IsDeleted())
			.OrderBy(account => account.Order).ThenBy(account => account.Id.ToString("D"), StringComparer.Ordinal))
		{
			if (!groups.TryGetValue(account.GroupId, out GroupInfo? group))
			{
				throw new GroupNotFoundException("The account group does not exist or is deleted.");
			}
			if (!activeCurrencies.TryGetValue(account.CurrencyId, out Currency? currency))
			{
				throw new ElementNotFoundException("The account currency does not exist or is deleted.");
			}
			result.Elements.Add(new AccountElementInfo
			{
				Id = account.Id, GroupId = account.GroupId, GroupName = group.Name,
				Name = account.Name, Description = account.Description, Order = account.Order,
				IsFavorite = account.IsFavorite, CurrencyId = currency.Id, CurrencyName = currency.Name
			});
		}
		return result;
	}
}
