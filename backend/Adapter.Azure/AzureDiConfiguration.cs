using Adapter.Azure.Access;
using Adapter.Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Specialized;
using Master.Contracts.Adapters;
using Master.Contracts.Access;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Shared.Contracts;

namespace Adapter.Azure;

/// <summary>
/// Registers Azure implementations selected by the executable host.
/// The host supplies authenticated clients for existing Master database blobs.
/// The mapping must return the same blob for the same dataset across all workers.
/// Blob provisioning and credential selection remain host responsibilities.
/// The access implementation is shared across request scopes.
/// Storage sessions reuse the acquired access handle and the same blob mapping.
/// Each acquired handle owns an independent lease and renewal lifetime.
/// Registration performs no network access or synchronization workflow.
/// MasterService remains responsible for acquiring and disposing access handles.
/// </summary>
public static class AzureDiConfiguration
{
	public static void AddAdapterAzureModule(this IServiceCollection services, Func<string, BlobClient> getMasterBlob)
	{
		ArgumentNullException.ThrowIfNull(getMasterBlob);
		services.AddSingleton<IMasterLock>(provider => new AzureMasterLock(
			key => getMasterBlob(key).GetBlobLeaseClient(Guid.NewGuid().ToString()), TimeProvider.System,
			provider.GetService<ILogger<AzureMasterLockHandle>>() ?? NullLogger<AzureMasterLockHandle>.Instance));
		services.AddScoped<IMasterStorageAdapter>(provider => new BlobMasterStorageAdapter(provider.GetRequiredService<ISharedContext>(), getMasterBlob,
			provider.GetService<ILogger<BlobMasterStorageAdapter>>() ?? NullLogger<BlobMasterStorageAdapter>.Instance));
	}
}
