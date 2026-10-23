namespace Master.Contracts.Adapters.MasterStorage;

/// <summary>
/// Exposes the usable database file and publication boundary for one storage session.
/// StoragePath is consumed by the DAL when constructing its SQLite connection.
/// Publish receives a completed standalone snapshot with no required WAL sidecar.
/// MasterService coordinates snapshot creation and transactions through its unit of work.
/// Local publication is a no-op because transactions already update the authoritative file.
/// Remote publication replaces the authoritative object under the acquired access ownership.
/// A failed remote publication invalidates the session; reopen and inspect receipts on retry.
/// Disposal cleans up owned temporary files but neither releases access nor deletes the Local database.
/// </summary>
public interface IMasterStorageSession : IAsyncDisposable
{
	public string StoragePath { get; }

	public Task Publish(string snapshotPath, CancellationToken cancellationToken = default);
}
