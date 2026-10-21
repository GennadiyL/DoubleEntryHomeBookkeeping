using Setup.Contracts.Services.SystemConfigs;

namespace Setup.Contracts.Services;

/// <summary>
/// Provides settings-screen access to the single System configuration.
/// Initialization settings and Master revision are never edited by Local.
/// </summary>
public interface ISystemConfigService
{
	/// <summary>
	/// Returns the single System configuration for the settings screen without an identity parameter.
	/// This read does not call AcceptChanges.
	/// </summary>
	public Task<SystemConfigurationInfo> GetConfiguration(CancellationToken cancellationToken = default);
}
