using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Base.Services;

public interface IFavoriteService<T>
	where T : IFavoriteEntity
{
	public Task SetFavoriteStatus(Guid entityId, bool isFavorite);
}
