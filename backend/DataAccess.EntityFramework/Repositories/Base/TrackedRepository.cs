using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;
using DataAccess.Contracts.Repositories.Base;
using DataAccess.Core.Behaviors;
using DataAccess.Core.Entities;
using DataAccess.Core.EntityFramework.Behaviors;
using Microsoft.EntityFrameworkCore;

namespace DataAccess.EntityFramework.Repositories.Base;

/// <summary>
/// Implements database-side selection and bulk acceptance of tracked entities.
/// Keeps synchronization metadata dependencies outside the generic Core repository.
/// New rows qualify by their null edit revision regardless of flags.
/// Existing rows qualify through any uncaptured modification flags.
/// Deletion state does not exclude a qualifying row.
/// Derived aggregate repositories extend the query with owned entry loading.
/// Results are detached business models mapped after one database query.
/// Bulk acceptance clears flags and replaces null edit revisions with the zero sentinel.
/// Bulk deletion removes local deletion markers using the configured database cascades.
/// The caller owns durability, dependency ordering, transaction lifetime and tracking-scope freshness.
/// </summary>
internal abstract class TrackedRepository<TB, TD> : Repository<AppDbContext, TB, TD>, ITrackedRepository<TB>
	where TB : class, IBaseEntity, ITrackedEntity
	where TD : class, IDalEntity, new()
{
	protected TrackedRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	protected IQueryable<TD> Changes => Entities.AsNoTracking().Where(entity =>
		EF.Property<long?>(entity, nameof(ITrackedEntity.EditRevision)) == null ||
		EF.Property<ModificationType>(entity, nameof(ITrackedEntity.ModificationType)) != ModificationType.None);

	public virtual async Task<ICollection<TB>> GetChanges(CancellationToken cancellationToken = default)
	{
		List<TD> entities = await Changes.ToListAsync(cancellationToken);
		return Mapper.Map<TD, TB>(entities);
	}

	public Task<int> HardDelete(CancellationToken cancellationToken = default) =>
		Entities.Where(entity => EF.Property<long?>(entity, nameof(ITrackedEntity.DeleteRevision)) == 0)
			.ExecuteDeleteAsync(cancellationToken);

	public Task<int> AcceptChanges(CancellationToken cancellationToken = default) =>
		Changes.ExecuteUpdateAsync(setters => setters
			.SetProperty(entity => EF.Property<long?>(entity, nameof(ITrackedEntity.EditRevision)),
				entity => EF.Property<long?>(entity, nameof(ITrackedEntity.EditRevision)) ?? 0L)
			.SetProperty(entity => EF.Property<ModificationType>(entity, nameof(ITrackedEntity.ModificationType)),
				ModificationType.None), cancellationToken);
}
