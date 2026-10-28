using Business.Models.Entities.Config;
using Business.Models.Entities.Synchronizations;

namespace DataAccess.Contracts;

/// <summary>
/// Identifies the Local database unit of work within the application container.
/// Inherits the common repository and transaction contract.
/// Consumers select this interface when they require Local persistence.
/// Its repositories must use the same context as its transaction operations.
/// Implementations retain scoped lifetime and lazy repository resolution.
/// The contract exposes business models without EF Core dependencies.
/// Snapshot preparation binds an unused scope to a supplied candidate database.
/// Activation atomically copies a prepared snapshot into the active Local database.
/// </summary>
public interface ILocalAppUnitOfWork : IAppUnitOfWork
{
	public Task OpenSnapshot(string snapshotPath, CancellationToken cancellationToken = default);
	public Task ReplaceLocalState(LocalConfig configuration, LocalJson pending, CancellationToken cancellationToken = default);
	public Task InstallSnapshot(string snapshotPath, CancellationToken cancellationToken = default);
}
