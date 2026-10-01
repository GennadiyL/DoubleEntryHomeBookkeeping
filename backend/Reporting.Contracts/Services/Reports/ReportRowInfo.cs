namespace Reporting.Contracts.Services.Reports;

/// <summary>
/// Returns one calculated report presentation row.
/// Key is a display grouping key rather than a persistent entity identity.
/// Label supplies the row caption.
/// CurrencyCode identifies an own-currency amount where applicable.
/// Amount is null when a mixed-currency own total is not meaningful.
/// BaseAmount sums individually rounded entry base amounts.
/// Level describes hierarchy depth for presentation.
/// The row provides no transaction drill-through or stored balance.
/// </summary>
public record ReportRowInfo
{
	public required string Key { get; set; }
	public required string Label { get; set; }
	public string? CurrencyCode { get; set; }
	public decimal? Amount { get; set; }
	public decimal BaseAmount { get; set; }
	public int Level { get; set; }
}
