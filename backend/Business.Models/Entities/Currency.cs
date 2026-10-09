using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

/// <summary>
/// Represents a currency available within a bookkeeping dataset.
/// Stores its unique immutable ISO code, editable short name and immutable catalog metadata.
/// Accounts and System configuration reference its identity.
/// Each currency has exactly one protected initial fallback rate plus ordinary dated rates.
/// The dataset base currency and currencies used by accounts cannot be deleted.
/// Favorite changes are content changes; currency ordering is a separate zero-based sequence.
/// New rows have null revisions and None modification flags.
/// Services enforce lifecycle and persistence; the model carries data.
/// </summary>
public class Currency : BaseEntity, ITrackedEntity, IFavoriteEntity, IOrderedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public required string Code { get; set; }
	public required string Symbol { get; set; }
	public required string EnglishName { get; set; }
	public required string Name { get; set; }
	public bool IsFavorite { get; set; }
	public int Order { get; set; }
	public List<CurrencyRate> Rates { get; set; } = new();
}
