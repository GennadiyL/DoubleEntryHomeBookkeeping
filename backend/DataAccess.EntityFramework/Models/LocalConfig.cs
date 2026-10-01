using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class LocalConfig : IDalEntity
{
	public Guid Id { get; set; }
	public string LocalDatasetKey { get; set; } = string.Empty;
	public AccountNameOrder AccountNameOrder { get; set; } = AccountNameOrder.CorrespondentCategoryProject;
	public ConflictPriority ConflictPriority { get; set; } = ConflictPriority.Local;
	public SyncTrigger SyncTrigger { get; set; } = SyncTrigger.ManualOnly;
	public long SnapshotRevision { get; set; }
	public string DefaultAccountNameSeparator { get; set; } = "/";
}
