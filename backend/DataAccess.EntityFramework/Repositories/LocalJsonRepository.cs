using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using LocalJsonEntity = Business.Models.Entities.Synchronizations.LocalJson;

namespace DataAccess.EntityFramework.Repositories;

/// <summary>
/// Provides detached repository access to the Local synchronization JSON document.
/// Maps the business entity to its internal DAL representation.
/// Uses the context selected by the database role's keyed registration.
/// Reads return the stored JSON and identity without interpreting the payload.
/// Writes participate in the caller-owned unit-of-work transaction.
/// Capture acceptance can share that transaction with document persistence.
/// The workflow owns singleton selection, validation and lifecycle decisions.
/// This repository performs no file access or remote synchronization.
/// </summary>
internal sealed class LocalJsonRepository : Repository<AppDbContext, LocalJsonEntity, LocalJson>, ILocalJsonRepository
{
	public LocalJsonRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}
}
