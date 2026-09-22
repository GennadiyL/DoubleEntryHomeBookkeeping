using Business.Models.Entities;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;

namespace DataAccess.Contracts.Repositories;

public interface IAccountRepository : IElementRepository<AccountGroup, Account>
{
}

