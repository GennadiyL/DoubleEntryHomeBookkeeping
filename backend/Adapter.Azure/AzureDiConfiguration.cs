using Adapter.Azure.Storage;
using Master.Contracts.Adapters;
using Microsoft.Extensions.DependencyInjection;

namespace Adapter.Azure;

/// <summary>
/// Registers Master storage adapters selected by the executable host.
/// Consumes storage abstractions without choosing their provider.
/// The host separately registers shared services and its storage factories.
/// Sessions use the processor configuration captured when they are opened.
/// MasterService remains responsible for acquiring and disposing storage locks.
/// Registration performs no storage access or synchronization workflow.
/// Azure SDK dependencies remain inside the Shared.Azure provider.
/// Temporary database management remains the responsibility of the adapter.
/// </summary>
public static class AzureDiConfiguration
{
	public static void AddAdapterAzureModule(this IServiceCollection services)
	{
		services.AddScoped<IMasterStorageAdapter, BlobMasterStorageAdapter>();
	}
}
