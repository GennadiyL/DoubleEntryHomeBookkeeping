namespace Shared.Contracts.Tls;

/// <summary>
/// Defines the thread-local context data.
/// Carries correlation data associated with the current logical execution context.
/// The shared TLS service creates and exposes an instance for the active flow.
/// Infrastructure and business-independent services can use the correlation identifier consistently.
/// It contains no business-domain state or logging behavior.
/// </summary>
public class TlsData
{
	public string CorrelationId { get; set; } = Guid.NewGuid().ToString("N");
}
