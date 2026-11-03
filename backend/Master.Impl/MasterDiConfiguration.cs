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
/// Batch operations resolve the same Master unit from the scoped factory.
/// Provider-specific registration remains the infrastructure host's choice.
/// No nested container, database file or transaction is created here.
/// Shared services must be registered by the composing host.
/// The scoped Master service coordinates submission transactions and receipts.
/// </summary>
public static class MasterDiConfiguration
{
	public static void AddMasterModule(this IServiceCollection services)
	{
		services.AddScoped<IMasterService, MasterService>();
		services.AddScoped<IApplySyncBatchOperation, ApplySyncBatchOperation>();
		services.AddScoped<IMasterStorageOperation, MasterStorageOperation>();
	}
}
