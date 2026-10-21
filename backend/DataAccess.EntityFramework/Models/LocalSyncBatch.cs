using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

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
internal class LocalSyncBatch : IDalEntity
{
	public Guid Id { get; set; }
	public Guid LocalRegistrationId { get; set; }
	public LocalRegistration LocalRegistration { get; set; } = null!;
	public string SyncKey { get; set; } = string.Empty;
	public long Revision { get; set; }
	public string PayloadHash { get; set; } = string.Empty;
}
