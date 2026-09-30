using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities;

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
