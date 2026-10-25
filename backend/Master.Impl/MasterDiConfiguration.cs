using DataAccess.Contracts;
using DataAccess.Contracts.Commands;
using Master.Contracts.Services;
using Master.Impl.Operations.ApplySyncBatch;
using Master.Impl.Operations.MasterStorage;
using Master.Impl.Services;
using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;

namespace Master.Impl;

/// <summary>
/// Registers Master-owned business operations in the host container.
/// Implementations remain internal to the Master implementation assembly.
/// Operations resolve the typed Master unit of work in the current scope.
/// The cumulative command uses the same Master role registration.
/// Provider-specific registration remains the infrastructure host's choice.
/// No nested container, database file or transaction is created here.
/// Shared services must be registered by the composing host.
/// The scoped Master service coordinates submission transactions and receipts.
/// </summary>
public static class MasterDiConfiguration
{
	public static void AddLocalMasterStorage(this IServiceCollection services, Func<string, string> getDatabasePath)
	{
		ArgumentNullException.ThrowIfNull(getDatabasePath);
		services.AddScoped<IMasterStorageOperation>(provider => new LocalMasterStorageOperation(
			provider.GetRequiredService<ISharedContext>(), getDatabasePath));
	}

	public static void AddRemoteMasterStorage(this IServiceCollection services)
	{
		services.AddScoped<IMasterStorageOperation, RemoteMasterStorageOperation>();
	}

	public static void AddMasterModule(this IServiceCollection services)
	{
		services.AddScoped<IMasterService, MasterService>();
		services.AddScoped<IApplySyncBatchOperation>(provider => new ApplySyncBatchOperation(
			provider.GetRequiredService<ISharedContext>(), provider.GetRequiredService<IMasterAppUnitOfWork>(),
			provider.GetRequiredKeyedService<ICumulativeAmountCommand>(typeof(IMasterAppUnitOfWork))));
	}
}
