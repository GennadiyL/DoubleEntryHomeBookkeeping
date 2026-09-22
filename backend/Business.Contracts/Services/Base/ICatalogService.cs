using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Services.Base;

public interface ICatalogService<T, in TParam> : IEntityService<TParam>, IOrderedService<T>, IFavoriteService<T>
	where T : class, ICatalogEntity
	where TParam : class
{
}
