using Business.Models.Entities.Interfaces;

namespace Business.Contracts.Services.Base;

public interface IOrderedService<T>
	where T : IOrderedEntity
{
	public Task SetOrder(Guid entityId, int order);
}
