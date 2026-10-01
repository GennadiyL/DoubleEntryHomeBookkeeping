using Setup.Contracts.Services.LocalConfigs;

namespace Setup.Contracts.Services;

/// <summary>
/// Provides settings-screen access to the single Local configuration.
/// Reads do not save changes and saves preserve the existing configuration identity.
/// </summary>
public interface ILocalConfigService
{
	/// <summary>
	/// Returns the single Local configuration for the settings screen without an identity parameter.
	/// This read does not call AcceptChanges.
	/// </summary>
	public Task<LocalConfigurationInfo> GetConfiguration(CancellationToken cancellationToken = default);

	/// <summary>
	/// Validates and saves editable settings to the single Local configuration.
	/// Commits a successful state-changing action once through AcceptChanges; invalid inputs do not save.
	/// Local settings do not synchronize.
	/// </summary>
	public Task SaveConfiguration(SaveLocalConfiguration settings, CancellationToken cancellationToken = default);
}
