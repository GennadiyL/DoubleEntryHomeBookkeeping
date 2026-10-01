using Reporting.Contracts.Services.Reports;

namespace Reporting.Contracts.Services;

/// <summary>
/// Provides report calculation and saved-definition maintenance.
/// The operations implement the logical reporting API/BFF family.
/// Report definitions store instructions rather than computed results.
/// Calculation uses current classifications and Confirmed transactions.
/// Calendar behavior follows the supplied current device timezone.
/// Reading and running preserve stored selection intent.
/// Only explicit saves normalize edited selection overrides.
/// Deleting a definition never deletes bookkeeping records.
/// </summary>
public interface IReportsService
{
	/// <summary>
	/// Calculates selected totals for the report result screen without modifying the saved definition.
	/// </summary>
	public Task<ReportCalculationInfo> Calculate(CalculateReport command);

	/// <summary>
	/// Reads saved definitions for the report chooser while preserving missing identities and saved selection intent.
	/// </summary>
	public Task<List<ReportInfo>> GetDefinitions();

	/// <summary>
	/// Creates or updates a definition from an explicit editor save and returns the saved projection.
	/// </summary>
	public Task<ReportInfo> SaveDefinition(SaveReportDefinition command);

	/// <summary>
	/// Deletes a saved definition from the report chooser without changing bookkeeping records.
	/// </summary>
	public Task DeleteDefinition(Guid reportId);
}
