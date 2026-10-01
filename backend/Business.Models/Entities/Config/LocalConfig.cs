using Business.Core.Entities;
using Business.Models.Enums;

namespace Business.Models.Entities.Config;

/// <summary>
/// Stores settings and installation state belonging to one local database copy.
/// LocalDatasetKey identifies the registered copy; SnapshotRevision records the installed snapshot.
/// Name order, separator, sync trigger, and conflict priority hold local preferences.
/// Settings survive database replacement and do not synchronize as shared business content.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class LocalConfig : BaseEntity
{
	public string LocalDatasetKey { get; set; } = string.Empty;
	public AccountNameOrder AccountNameOrder { get; set; } = AccountNameOrder.CorrespondentCategoryProject;
	public string DefaultAccountNameSeparator { get; set; } = "/";
	public ConflictPriority ConflictPriority { get; set; } = ConflictPriority.Local;
	public long SnapshotRevision { get; set; }
	public SyncTrigger SyncTrigger { get; set; } = SyncTrigger.ManualOnly;
}
