namespace Business.Contracts.Services;

/// <summary>
/// Exposes cumulative rebuilding to synchronization and other outer clients.
/// A rebuild covers every live account before normal database use.
/// Derived cache writes do not mark transactions as edited.
/// Each public action owns its database transaction through the unit of work.
/// Business operations and DAL commands participate without owning that transaction.
/// Any failure rolls back the complete rebuild.
/// This contract does not download or publish database snapshots.
/// Callers must not expose a downloaded database until rebuilding succeeds.
/// </summary>
public interface ICumulativeService
{
	/// <summary>
	/// Rebuilds all account cumulative amounts before a downloaded database is made available.
	/// Commits the complete rebuild or rolls back all its writes on failure.
	/// </summary>
	public Task Rebuild(CancellationToken cancellationToken = default);
}
