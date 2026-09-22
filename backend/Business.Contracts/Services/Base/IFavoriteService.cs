using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Services.Base;

public interface IFavoriteService<T>
	where T : IFavoriteEntity
{
	public Task SetFavoriteStatus(Guid entityId, bool isFavorite);
}
