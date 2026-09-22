using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class CorrespondentService : ElementService<CorrespondentGroup, Correspondent>, ICorrespondentService
{
	private readonly IAccountRepository _accountRepository;

	public CorrespondentService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.CorrespondentRepo, unitOfWork.CorrespondentGroupRepo)
	{
		_accountRepository = unitOfWork.AccountRepo;
	}

	protected override Task<ICollection<Account>> GetReferencingAccounts(Guid elementId) =>
		_accountRepository.GetByCorrespondentIdAsync(elementId);

	protected override void ReplaceAccountReference(Account account, Correspondent destination)
	{
		account.CorrespondentId = destination.Id;
		account.Correspondent = destination;
	}
}
