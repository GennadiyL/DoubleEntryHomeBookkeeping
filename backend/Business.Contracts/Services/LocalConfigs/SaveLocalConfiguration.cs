using Business.Models.Enums;

namespace Business.Contracts.Services.LocalConfigs;

/// <summary>
/// Contains editable settings for the single Local configuration.
/// Used by SaveConfiguration without an identity parameter.
/// The existing persistent configuration identity is retained.
/// These settings do not synchronize.
/// Dataset keys and synchronization bookkeeping are excluded.
/// The service validates settings before changing stored values.
/// A successful state-changing save commits once through AcceptChanges.
/// Invalid settings do not persist partial changes.
/// </summary>
public record SaveLocalConfiguration
{
	public AccountNameOrder AccountNameOrder { get; set; } = AccountNameOrder.CorrespondentCategoryProject;
	public string DefaultAccountNameSeparator { get; set; } = "/";
	public ConflictPriority ConflictPriority { get; set; } = ConflictPriority.Local;
	public SyncTrigger SyncTrigger { get; set; } = SyncTrigger.ManualOnly;
}
