using Microsoft.Extensions.DependencyInjection;

namespace Messaging.InProcessBus;

/// <summary>
/// Defines the in-process messaging composition entry point.
/// Registers business-specific message handlers for local in-process dispatch.
/// Local hosts call this module together with the default shared messaging implementation.
/// The registrations connect Messaging.Contracts message types to business service handlers.
/// It does not register or communicate with a remote service bus.
/// </summary>
public static class InProcessBusDiConfiguration
{
	public static void AddMessagingInProcessBusModule(this IServiceCollection services)
	{
	}
}
