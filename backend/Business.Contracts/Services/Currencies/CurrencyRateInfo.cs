namespace Business.Contracts.Services.Currencies;

/// <summary>
/// Describes one currency rate returned for the rate editor.
/// Date is a calendar date, not a UTC timestamp.
/// The currency and date identify the rate for upsert.
/// Results include the initial rate and use descending date order.
/// IsInitial allows the UI to hide its special date and protect deletion.
/// No persistent row identity is required by the editor.
/// Rates already stored in transaction entries remain independent.
/// Changing this record requires a separate AddOrUpdate call to persist.
/// </summary>
public record CurrencyRateInfo
{
	public Guid CurrencyId { get; set; }
	public DateOnly Date { get; set; }
	public decimal Rate { get; set; }
	public string? Description { get; set; }
	public bool IsInitial { get; set; }
}
