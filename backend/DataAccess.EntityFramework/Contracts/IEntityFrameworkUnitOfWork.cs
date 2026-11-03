using Microsoft.EntityFrameworkCore.Infrastructure;

namespace DataAccess.EntityFramework.Contracts;

public interface IEntityFrameworkUnitOfWork
{
	public DatabaseFacade Database { get; }
}
