using Microsoft.EntityFrameworkCore;
using DataAccess.Contracts.Repositories;
using DataAccess.Core.Behaviors;
using DataAccess.Core.EntityFramework.Behaviors;
using DataAccess.EntityFramework.Models;
using LocalSyncBatchEntity = Business.Models.Entities.Synchronizations.LocalSyncBatch;

namespace DataAccess.EntityFramework.Repositories;

/// <summary>
/// Records one batch successfully processed for a registered Local copy.
/// LocalRegistrationId links the receipt to its submitting registration.
/// SyncKey is the original immutable batch identity reused by retries.
/// The registration and SyncKey pair is unique within the Master database.
/// Revision identifies the committed Master version that accepted the batch.
/// PayloadHash detects reuse of an identity with different batch content.
/// The receipt commits in the same transaction as its business changes and revision.
/// This metadata is not included in outgoing Local change capture.
/// </summary>
internal sealed class LocalSyncBatchRepository : Repository<AppDbContext, LocalSyncBatchEntity, LocalSyncBatch>, ILocalSyncBatchRepository
{
	public LocalSyncBatchRepository(AppDbContext context, IMapper mapper) : base(context, mapper)
	{
	}

	public override async Task<LocalSyncBatchEntity?> GetById(Guid id, CancellationToken cancellationToken = default)
	{
		LocalSyncBatch? entity = await Entities.AsNoTracking().Include(item => item.LocalRegistration)
			.SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
		return entity is null ? null : Mapper.Map<LocalSyncBatch, LocalSyncBatchEntity>(entity);
	}
}
