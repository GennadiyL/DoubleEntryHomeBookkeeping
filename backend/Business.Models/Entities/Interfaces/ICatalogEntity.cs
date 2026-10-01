using Business.Core.Entities;

namespace Business.Models.Entities.Interfaces;

/// <summary>
/// Combines identity, naming, favorites, catalog ordering and synchronization metadata.
/// Implemented by named catalog groups and elements.
/// Content and position changes use separate modification bits.
/// The contract carries data; services enforce lifecycle and validation.
/// </summary>
public interface ICatalogEntity : IBaseEntity, ITrackedEntity, INamedEntity, IOrderedEntity, IFavoriteEntity
{
}
