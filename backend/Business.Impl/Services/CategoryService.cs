using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class CategoryService : ElementService<CategoryGroup, Category>, ICategoryService
{
	private readonly IAccountRepository _accountRepository;

	public CategoryService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.CategoryRepo, unitOfWork.CategoryGroupRepo)
	{
		_accountRepository = unitOfWork.AccountRepo;
	}

	protected override Task<ICollection<Account>> GetReferencingAccounts(Guid elementId, CancellationToken cancellationToken = default) =>
		_accountRepository.GetByCategoryIdAsync(elementId, cancellationToken);

	protected override void ReplaceAccountReference(Account account, Category destination)
	{
		account.CategoryId = destination.Id;
		account.Category = destination;
	}
}
