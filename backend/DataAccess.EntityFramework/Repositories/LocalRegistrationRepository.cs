using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using LocalRegistrationEntity = Business.Models.Entities.Synchronizations.LocalRegistration;

namespace DataAccess.EntityFramework.Repositories;

/// <summary>
/// Records one Local copy registered with this Master dataset.
/// LocalDatasetKey is stable and unique within the dataset.
/// SyncDateTime is the Master UTC timestamp of the last successful acknowledgement.
/// It remains null until the initial installation has been acknowledged.
/// SyncRevision is the latest installed revision confirmed to Master.
/// It can lag behind the actual Local revision while acknowledgement is pending.
/// Master uses these values for expiry and deletion-marker retention.
/// This metadata is not captured or merged as a Local business change.
/// </summary>
internal sealed class LocalRegistrationRepository : Repository<AppDbContext, LocalRegistrationEntity, LocalRegistration>, ILocalRegistrationRepository
{
	public LocalRegistrationRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
