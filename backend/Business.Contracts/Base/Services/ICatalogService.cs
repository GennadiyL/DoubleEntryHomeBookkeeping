using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

/// <summary>
/// Combines ordering and favorite operations for a named catalog entity.
/// Catalog identity, naming and tracking are defined by the entity contract.
/// Order changes synchronize separately from content changes.
/// Favorite changes participate in content synchronization.
/// </summary>
public interface ICatalogService<T> : IOrderedService<T>, IFavoriteService<T>
	where T : class, ICatalogEntity
{
}
