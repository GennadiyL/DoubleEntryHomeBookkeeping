using Business.Core.Entities;

namespace DataAccess.Core.Behaviors;

public interface IRepository<T>
	where T : class, IBaseEntity
{
	public void Add(T entity);

	public void Update(T entity);

	public Task<ICollection<T>> GetAll(CancellationToken cancellationToken = default);

	public Task<T?> GetById(Guid id, CancellationToken cancellationToken = default);

	public ICollection<T> GetAllSync();

	public T? GetByIdSync(Guid id);
}
