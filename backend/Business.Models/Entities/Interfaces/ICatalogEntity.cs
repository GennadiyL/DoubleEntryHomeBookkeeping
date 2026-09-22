using Business.Core.Entities;

namespace Business.Models.Entities.Interfaces;

public interface ICatalogEntity : IBaseEntity, ITrackedEntity, INamedEntity, IOrderedEntity, IFavoriteEntity
{
}
