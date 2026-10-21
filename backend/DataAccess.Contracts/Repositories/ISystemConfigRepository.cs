using Business.Models.Entities.Config;
using DataAccess.Contracts.Repositories.Base;

namespace DataAccess.Contracts.Repositories;

public interface ISystemConfigRepository : ITrackedRepository<SystemConfig>
{
}
