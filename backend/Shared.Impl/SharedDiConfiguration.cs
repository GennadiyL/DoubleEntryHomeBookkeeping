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

		services.AddSingleton<IDateTimeService, DateTimeService>();
		services.AddSingleton<IDirectoryService, DirectoryService>();
		services.AddSingleton<IFileService, FileService>();
		services.AddSingleton<IZipService, ZipService>();
		services.AddSingleton<ITlsService, TlsService>();
		services.AddSingleton<IXmlService, XmlService>();
		services.AddSingleton<IJsonService, JsonService>();

		services.AddSingleton<ILogService, NullLogService>();
		services.AddSingleton<IMessageService, InProcessBusMessageService>();
	}
}
