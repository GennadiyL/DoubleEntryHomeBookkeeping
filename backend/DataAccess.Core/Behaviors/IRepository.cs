using Business.Core.Entities;

namespace DataAccess.Core.Behaviors;

public interface IRepository<T>
	where T : class, IBaseEntity
{
	public void Add(T entity);

	public void Update(T entity);

	public Task<ICollection<T>> GetAllAsync(CancellationToken cancellationToken = default);

	public Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

	public ICollection<T> GetAll();

	public T? GetById(Guid id);
}
