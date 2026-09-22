using Business.Core.Entities;
using Business.Models.Entities.Interfaces;

namespace Business.Models.Entities;

public class Currency : BaseEntity, ITrackedEntity, IFavoriteEntity, IOrderedEntity
{
	public DateTime Original { get; set; }
	public DateTime Current { get; set; }
	public bool IsDeleted { get; set; }
	public required string IsoCode { get; set; }
	public required string Symbol { get; set; }
	public required string Name { get; set; }
	public bool IsFavorite { get; set; }
	public int Order { get; set; }
	public List<CurrencyRate> Rates { get; set; } = new();
}
