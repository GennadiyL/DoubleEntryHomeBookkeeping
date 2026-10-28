using DataAccess.Contracts;

namespace DataAccess.EntityFramework.SqLite;

/// <summary>
/// Provides Master repository and transaction access for the scoped factory.
/// Uses the common AppDbContext without a role-specific context subclass.
/// The factory fixes the database path before resolving this instance.
/// Repository properties resolve lazily through the current scoped service provider.
/// Every repository participates in the same context and transaction.
/// The factory caches this unit for the current API operation.
/// The factory disposes the context when its API scope ends.
/// File creation, publication and deletion are outside this unit.
/// </summary>
internal sealed class MasterUnitOfWork : AppUnitOfWork, IMasterUnitOfWork
{
	public MasterUnitOfWork(
		AppDbContext context,
		IServiceProvider serviceProvider,
		string masterDatasetKey) :
		base(context, serviceProvider)
	{
		MasterDatasetKey = masterDatasetKey;
	}

	public string MasterDatasetKey { get; }
}
