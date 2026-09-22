using Microsoft.Extensions.DependencyInjection;
using Shared.Contracts;
using Tests.Common.Mocks.Shared;

namespace Tests.Common.DiConfigurations;

public static class SharedMockDiConfiguration
{
	public static void AddSharedMockModule(this IServiceCollection services)
	{
		services.AddScoped<IDateTimeService, MockDateTimeService>();
		services.AddScoped<IDirectoryService, MockDirectoryService>();
		services.AddScoped<IFileService, MockFileService>();
		services.AddScoped<IJsonService, MockJsonService>();
		services.AddScoped<ILogService, MockLogService>();
	}
}
