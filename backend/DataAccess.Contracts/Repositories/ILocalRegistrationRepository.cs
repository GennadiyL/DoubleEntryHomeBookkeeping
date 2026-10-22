using Business.Models.Entities.Synchronizations;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface ILocalRegistrationRepository : IRepository<LocalRegistration>
{
	public Task<LocalRegistration?> GetByLocalDatasetKey(string localDatasetKey, CancellationToken cancellationToken = default);
}
