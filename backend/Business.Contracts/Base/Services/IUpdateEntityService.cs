namespace Business.Contracts.Base.Services;

public interface IUpdateEntityService<in T>
	where T : class
{
	public Task<Guid> Add(T param);
	public Task Update(Guid entityId, T param);
	public Task Delete(Guid entityId);
}
