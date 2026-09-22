using Business.Contracts.Services;
using Business.Impl.Services.Base;
using Business.Models.Entities;
using DataAccess.Contracts;
using DataAccess.Contracts.Repositories;
using Shared.Contracts;

namespace Business.Impl.Services;

internal sealed class ProjectService : ElementService<ProjectGroup, Project>, IProjectService
{
	private readonly IAccountRepository _accountRepository;

	public ProjectService(ISharedContext sharedContext, IAppUnitOfWork unitOfWork)
		: base(sharedContext, unitOfWork, unitOfWork.ProjectRepo, unitOfWork.ProjectGroupRepo)
	{
		_accountRepository = unitOfWork.AccountRepo;
	}

	protected override Task<ICollection<Account>> GetReferencingAccounts(Guid elementId) =>
		_accountRepository.GetByProjectIdAsync(elementId);

	protected override void ReplaceAccountReference(Account account, Project destination)
	{
		account.ProjectId = destination.Id;
		account.Project = destination;
	}
}
