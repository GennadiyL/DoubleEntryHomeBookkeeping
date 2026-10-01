using Business.Contracts.Services.Configs;

namespace Business.Contracts.Services;

/// <summary>
/// Provides a combined read-only view of System and Local settings.
/// Business operations use these settings for calculations and naming.
/// Both configuration rows belong to the currently opened dataset.
/// The result is a detached snapshot with entity IDs instead of navigation objects.
/// Reading configuration does not save changes or call AcceptChanges.
/// User-facing configuration writes belong to the Setup subdomain.
/// This contract has no dependency on Setup contracts or persistence models.
/// Dataset registration and synchronization tracking are outside this settings view.
/// </summary>
public interface IConfigService
{
	/// <summary>
	/// Returns all System and Local settings in one read-only snapshot for business operations.
	/// Reads the current configuration singletons without an ID parameter or persistence changes.
	/// Missing either singleton raises a critical exception; no partial or default settings are returned.
	/// </summary>
	public Task<ConfigurationInfo> GetConfiguration();
}
