using Business.Models.Entities;
using DataAccess.Contracts.Repositories.Base;

namespace DataAccess.Contracts.Repositories;

public interface ICurrencyRepository : ITrackedRepository<Currency>
{
}
