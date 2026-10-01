using Synchronization.Contracts.Services.Diagnostics;
using Synchronization.Contracts.Services.Synchronizations;

namespace Synchronization.Contracts.Services;

/// <summary>
/// Exposes session synchronization reporting and local diagnostic logging.
/// The latest report describes received changes only.
/// No persistent report history is promised.
/// Logs are stored locally with seven-day retention.
/// Sensitive authentication and transfer values must be redacted.
/// These operations do not grant access to blocked business data.
/// No sharing or exporting of logs is included.
/// Cleanup timing and platform log locations remain implementation decisions.
/// </summary>
public interface IDiagnosticsService
{
	/// <summary>
	/// Reads the latest session report for the result screen; null means no attempt report is available.
	/// </summary>
	public Task<SyncReportInfo?> GetLatestSyncReport(CancellationToken cancellationToken = default);

	/// <summary>
	/// Removes local logs older than the seven-day retention period and reports the cleanup outcome.
	/// </summary>
	public Task<OperationStatusInfo> CleanupLogs(CancellationToken cancellationToken = default);

	/// <summary>
	/// Writes a sanitized event to the local diagnostic log and reports its outcome.
	/// </summary>
	public Task<OperationStatusInfo> WriteDiagnostic(WriteDiagnostic command, CancellationToken cancellationToken = default);
}
