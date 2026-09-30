using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

/// <summary>
/// Represents a currency available within a bookkeeping dataset.
/// Stores its ISO code, display name, symbol, favorite flag, and catalog order.
/// Its rates collection holds exchange rates; accounts and System configuration reference its identity.
/// Services enforce currency uniqueness, base-currency protection, and deletion rules.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class Currency : BaseEntity, ITrackedEntity, IFavoriteEntity, IOrderedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public required string Code { get; set; }
	public required string Symbol { get; set; }
	public required string Name { get; set; }
	public bool IsFavorite { get; set; }
	public int Order { get; set; }
	public List<CurrencyRate> Rates { get; set; } = new();
}
