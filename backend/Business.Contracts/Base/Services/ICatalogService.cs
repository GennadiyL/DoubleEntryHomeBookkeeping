using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

public interface ICatalogService<T> : IOrderedService<T>, IFavoriteService<T>
	where T : class, ICatalogEntity
{
}
