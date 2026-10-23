namespace DataAccess.Contracts;

/// <summary>
/// Identifies the Master database unit of work within the application container.
/// Inherits the common repository and transaction contract.
/// Consumers select this interface when they require Master persistence.
/// Its repositories must use the same context as its transaction operations.
/// Implementations retain scoped lifetime and lazy repository resolution.
/// The contract exposes business models without EF Core dependencies.
/// The service supplies a prepared database path before beginning synchronization.
/// Snapshot creation and connection cleanup expose only local database operations.
/// Blob storage, leases and publication remain outside this persistence contract.
/// Infrastructure registers the matching provider implementation.
/// </summary>
public interface IMasterAppUnitOfWork : IAppUnitOfWork
{
	public Task OpenSession(string storagePath, CancellationToken cancellationToken = default);

	/// <summary>Disposes the scoped database context; subsequent work requires a new scope.</summary>
	public Task CloseSession();

	public Task CreateSnapshot(string snapshotPath, CancellationToken cancellationToken = default);

}
