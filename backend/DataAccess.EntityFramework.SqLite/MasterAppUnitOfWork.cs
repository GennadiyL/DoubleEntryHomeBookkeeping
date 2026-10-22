using DataAccess.Contracts;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Implements the Master database unit of work using the shared EF behavior.
/// Receives MasterAppDbContext directly through its typed constructor.
/// Repository resolution uses the same key within the current scope.
/// All repository changes participate in this context's transactions.
/// The other database role has independent repositories and tracking state.
/// The SQLite module registers this provider-specific implementation.
/// This class does not create containers or own application configuration.
/// The dependency scope manages the context lifetime.
/// </summary>
internal sealed class MasterAppUnitOfWork : AppUnitOfWork, IMasterAppUnitOfWork
{
	public MasterAppUnitOfWork(
		MasterAppDbContext context,
		IServiceProvider serviceProvider)
		: base(context, serviceProvider, typeof(IMasterAppUnitOfWork))
	{
	}
}
