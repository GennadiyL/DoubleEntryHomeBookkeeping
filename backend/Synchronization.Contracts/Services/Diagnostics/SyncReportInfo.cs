namespace Synchronization.Contracts.Services.Diagnostics;

/// <summary>
/// Contains the latest synchronization attempt report for the current session.
/// Used by the synchronization result screen.
/// SyncKey is absent when no durable attempt was allocated.
/// Items summarize received changes by entity family.
/// Messages include Currency reordering once when applicable.
/// A new attempt replaces the prior session report.
/// The report is discarded on application closure.
/// It is not a persistent business entity or an uploaded log.
/// </summary>
public record SyncReportInfo
{
	public string? SyncKey { get; set; }
	public List<SyncReportItemInfo> Items { get; } = new();
	public List<string> Messages { get; } = new();
}
