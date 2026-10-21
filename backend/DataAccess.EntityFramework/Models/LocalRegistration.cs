using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

/// <summary>
/// Records one Local copy registered with this Master dataset.
/// LocalDatasetKey is stable and unique within the dataset.
/// SyncDateTime is the Master UTC timestamp of the last successful acknowledgement.
/// It remains null until the initial installation has been acknowledged.
/// SyncRevision is the latest installed revision confirmed to Master.
/// It can lag behind the actual Local revision while acknowledgement is pending.
/// Master uses these values for expiry and deletion-marker retention.
/// This metadata is not captured or merged as a Local business change.
/// </summary>
internal class LocalRegistration : IDalEntity
{
	public Guid Id { get; set; }
	public string LocalDatasetKey { get; set; } = string.Empty;
	public DateTime? SyncDateTime { get; set; }
	public long SyncRevision { get; set; }
}
