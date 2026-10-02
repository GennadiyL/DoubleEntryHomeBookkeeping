using Business.Contracts.Services.Configs;
using Business.Models.Entities.Config;

namespace Business.Impl.Operations.Config;

/// <summary>
/// Shares configuration access within the Business implementation.
/// Supplies the combined settings snapshot exposed by the config service.
/// Loads individual singletons for operations requiring only one settings scope.
/// Missing or invalid configuration raises a critical failure.
/// Services share this operation instead of calling another service.
/// Supports balancing-account cleanup as part of an existing business mutation.
/// Configuration writes remain pending until the owning service commits.
/// User-facing settings editing belongs to the Setup subdomain.
/// </summary>
internal interface IConfigOperation
{
	public Task<ConfigurationInfo> GetConfiguration(CancellationToken cancellationToken = default);
	public Task<SystemConfig> GetSystemConfig(CancellationToken cancellationToken = default);
	public Task<LocalConfig> GetLocalConfig(CancellationToken cancellationToken = default);
}
