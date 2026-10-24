using Azure.Core;
using Azure.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Shared.Azure.Blob;
using Shared.Contracts.Storage;

namespace Shared.Azure;

/// <summary>
/// Registers the Azure storage factory selected by the executable host.
/// Register after AddSharedModule to replace the Local factory.
/// The host supplies IConfiguration loaded from appsettings.json and any overrides.
/// DefaultAzureCredential is reused unless the host registers another TokenCredential.
/// Factories create processors with their configured container clients.
/// Registration performs no network access or container creation.
/// </summary>
public static class SharedAzureDiConfiguration
{
	public static void AddSharedAzureModule(this IServiceCollection services)
	{
		services.TryAddSingleton<TokenCredential>(_ => new DefaultAzureCredential());
		services.AddScoped<IStorageProcessorFactory>(provider => new BlobStorageProcessorFactory(
			provider.GetRequiredService<IConfiguration>(),
			provider.GetRequiredService<TokenCredential>()));
	}
}
