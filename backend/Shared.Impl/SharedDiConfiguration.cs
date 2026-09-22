using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Shared.Impl.Logging;
using Shared.Impl.Messaging;
using Shared.Impl.Services;

namespace Shared.Impl;

/// <summary>
/// Defines the shared-services composition entry point.
/// Registers default project-independent service implementations and the shared context.
/// Every infrastructure host calls this module during application startup.
/// It selects the null logger and in-process publisher as default replaceable implementations.
/// Third-party implementations are registered by their own Shared technology projects.
/// </summary>
public static class SharedDiConfiguration
{
	public static void AddSharedModule(this IServiceCollection services)
	{
		services.AddScoped<ISharedContext, SharedContext>();

		services.AddScoped<IDateTimeService, DateTimeService>();
		services.AddScoped<IDirectoryService, DirectoryService>();
		services.AddScoped<IFileService, FileService>();
		services.AddScoped<IZipService, ZipService>();
		services.AddScoped<ITlsService, TlsService>();
		services.AddScoped<IXmlService, XmlService>();
		services.AddScoped<IJsonService, JsonService>();

		services.AddScoped<ILogService, NullLogService>();
		services.AddScoped<IMessageService, InProcessBusMessageService>();
	}
}
