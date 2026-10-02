using System.Text.Json;
using System.Text.Json.Serialization;
using Business.Impl;
using DataAccess.EntityFramework.SqLite;
using Messaging.InProcessBus;
using Shared.Impl;

namespace Dehb.WebApi;

/// <summary>
/// Defines the Web API composition entry point.
/// Registers the application modules and exception handling required by the web host.
/// The Web API startup path invokes this configuration while building services.
/// It composes business, data-access, shared, adapter, messaging, and HTTP concerns.
/// It does not map endpoints or execute requests.
/// </summary>
public static class WebApiDiConfiguration
{
	public static void AddWebApiDiConfiguration(this IServiceCollection services, IConfiguration configuration)
	{
		ConfigureJsonSerializerOptions(services);
		services.AddSharedModule();
		services.AddBusinessModule();
		services.AddDataAccessSqLiteModule(configuration);
		services.AddMessagingInProcessBusModule();
	}

	private static void ConfigureJsonSerializerOptions(IServiceCollection services)
	{
		services.ConfigureHttpJsonOptions(options =>
		{
			options.SerializerOptions.PropertyNameCaseInsensitive = true;
			options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
			options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
			options.SerializerOptions.PreferredObjectCreationHandling = JsonObjectCreationHandling.Populate;
		});
	}
}
