namespace Business.Contracts.Services.Currencies;

/// <summary>
/// Describes one saved currency for browsing and selection.
/// The list includes the base currency and follows catalog Order.
/// Created by the service for presentation and editing.
/// Contains values rather than persistent entity references.
/// Identifiers refer to existing bookkeeping records.
/// Does not expose synchronization revisions or modification flags.
/// Reading this record does not save changes.
/// Persistence remains the responsibility of the corresponding mutation operation.
/// </summary>
public record CurrencyInfo
{
	public Guid Id { get; set; }
	public string Code { get; set; } = string.Empty;
	public string EnglishName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string Symbol { get; set; } = string.Empty;
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
}
