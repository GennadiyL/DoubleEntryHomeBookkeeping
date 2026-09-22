namespace Dehb.WebApi;

/// <summary>
/// Defines the Web API endpoint composition entry point.
/// Maps the solution HTTP endpoint groups onto the application route builder.
/// The web host invokes it once after constructing the application.
/// It delegates audit-event route definitions to AuditEventsEndpoint.
/// It contains no endpoint business logic.
/// </summary>
public static class WebApiEndpointsConfiguration
{
	public static void AddEndpointsConfiguration(this WebApplication app)
	{
	}
}
