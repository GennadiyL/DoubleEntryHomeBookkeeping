using Business.Models.Enums;

namespace Setup.Contracts.Services.LocalConfigs;

/// <summary>
/// Contains settings from the single Local configuration.
/// Used to populate the configuration screen.
/// No configuration identity is needed to select the singleton.
/// Settings remain local to this database copy.
/// Dataset keys and synchronization revisions are not settings fields.
/// The result does not expose persistent navigation objects.
/// Reading settings does not commit changes.
/// SaveConfiguration persists editable fields separately.
/// </summary>
public record LocalConfigurationInfo
{
	public AccountNameOrder AccountNameOrder { get; set; } = AccountNameOrder.CorrespondentCategoryProject;
	public string AccountNameSeparator { get; set; } = "/";
	public bool AccountNameAddCurrency { get; set; }
	public ConflictPriority ConflictPriority { get; set; } = ConflictPriority.Local;
	public SyncTrigger SyncTrigger { get; set; } = SyncTrigger.ManualOnly;
}
