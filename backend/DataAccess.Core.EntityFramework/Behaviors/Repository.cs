using Business.Core.Entities;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.Core.EntityFramework.Behaviors;

/// <summary>
/// Defines the Entity Framework repository base.
/// Implements common CRUD mapping between persistent business models and database entities.
/// Concrete repositories derive from it with a DbContext, business type, and data-access type.
/// It coordinates DbSet operations with the shared IMapper contract.
/// Domain-specific queries remain in concrete repositories.
/// </summary>
public abstract class Repository<TContext, TB, TD> : IRepository<TB>
	where TContext : DbContext
	where TB : class, IBaseEntity, new()
	where TD : class, IDalEntity, new()
{
	protected TContext Context { get; }

	protected IMapper Mapper { get; }

	protected DbSet<TD> Entities { get; }

	protected Repository(TContext context, IMapper mapper)
	{
		Context = context;
		Mapper = mapper;
		Entities = Context.Set<TD>();
	}

	public virtual void Add(TB entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		Entities.Add(Mapper.Map<TD, TB>(entity));
	}

	public virtual void Update(TB entity)
	{
		ArgumentNullException.ThrowIfNull(entity);
		Entities.Update(Mapper.Map<TD, TB>(entity));
	}

	public virtual async Task<ICollection<TB>> GetAllAsync(CancellationToken cancellationToken)
	{
		List<TD> entities = await Entities.ToListAsync(cancellationToken);
		return Mapper.Map<TD, TB>(entities);
	}

	public virtual async Task<TB?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
	{
		TD? entity = await Entities.FindAsync([id], cancellationToken);

		if (entity != null)
		{
			return Mapper.Map<TD, TB>(entity);
		}

		return null;
	}

	public virtual ICollection<TB> GetAll()
	{
		List<TD> entities = [.. Entities];
		return Mapper.Map<TD, TB>(entities);
	}

	public virtual TB? GetById(Guid id)
	{
		TD? entity = Entities.Find(id);

		if (entity != null)
		{
			return Mapper.Map<TD, TB>(entity);
		}

		return null;
	}
}
