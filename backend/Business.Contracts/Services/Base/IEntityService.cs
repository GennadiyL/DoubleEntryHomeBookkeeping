namespace Business.Contracts.Services.Base;

public interface IEntityService<in TParam>
	where TParam : class
{
	public Task<Guid> Add(TParam param);
	public Task Update(Guid entityId, TParam param);
	public Task Delete(Guid entityId);
}
