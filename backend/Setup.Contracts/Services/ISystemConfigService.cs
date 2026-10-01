using Setup.Contracts.Services.SystemConfigs;

namespace Setup.Contracts.Services;

/// <summary>
/// Provides settings-screen access to the single System configuration.
/// Reads do not save changes and saves preserve the existing configuration identity.
/// </summary>
public interface ISystemConfigService
{
	/// <summary>
	/// Returns the single System configuration for the settings screen without an identity parameter.
	/// This read does not call AcceptChanges.
	/// </summary>
	public Task<SystemConfigurationInfo> GetConfiguration();

	/// <summary>
	/// Validates and saves editable settings to the single System configuration.
	/// Commits a successful state-changing action once through AcceptChanges; invalid inputs do not save.
	/// System changes include sync tracking; base currency and precisions remain immutable.
	/// </summary>
	public Task SaveConfiguration(SaveSystemConfiguration settings);
}
