namespace Synchronization.Contracts.Services.Diagnostics;

/// <summary>
/// Supplies a diagnostic event for the local log folder.
/// Used by application error and synchronization diagnostics.
/// OccurredAt is the event UTC instant.
/// OperationKey is a correlation key, not a business entity identity.
/// Message summarizes the event and Detail supplies optional diagnostics.
/// Passwords, authorization handles and transfer tokens must be redacted.
/// Log entries do not change business content.
/// Retention is handled separately by CleanupLogs.
/// </summary>
public record WriteDiagnostic
{
	public DateTime OccurredAt { get; set; }
	public required string OperationKey { get; set; }
	public required string Message { get; set; }
	public string? Detail { get; set; }
}
