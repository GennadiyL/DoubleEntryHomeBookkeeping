using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

public interface ICatalogService<T, in TParam> : IUpdateEntityService<TParam>, IOrderedService<T>, IFavoriteService<T>
	where T : class, ICatalogEntity
	where TParam : class
{
}
