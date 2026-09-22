using DataAccess.Contracts;
using DataAccess.Core.EntityFramework.Entities;

namespace DataAccess.EntityFramework;

/// <summary>
/// Defines the application unit of work.
/// Exposes application repositories while reusing the Entity Framework transaction base.
/// Business services receive one scoped instance through IAppUnitOfWork.
/// It resolves repositories lazily from the same service scope as AppDbContext.
/// It does not contain business rules or provider registration.
/// </summary>
internal class AppUnitOfWork : UnitOfWork<AppDbContext>, IAppUnitOfWork
{
	public AppUnitOfWork(AppDbContext context, IServiceProvider serviceProvider)
		: base(context, new Lazy<IServiceProvider>(() => serviceProvider))
	{
	}

	// public AppUnitOfWork(AppDbContext context, Lazy<IServiceProvider> serviceProvider) : base(context, serviceProvider)
	// {
	// }

}
