namespace DataAccess.Contracts;

public interface IUnitOfWorkFactory
{
	/// <summary>
	/// Gets the Local unit of work cached for the current API operation.
	/// Its lifetime is owned by the factory's dependency scope.
	/// </summary>
	public ILocalUnitOfWork CreateLocal();

	/// <summary>
	/// Gets the Master unit of work cached for the dataset in the current API operation.
	/// Its lifetime is owned by the factory's dependency scope.
	/// </summary>
	public IMasterUnitOfWork CreateMaster(string masterDatasetKey);

	/// <summary>
	/// Gets the Master copy in an operation-specific directory, including isolated downloads.
	/// </summary>
	public IMasterUnitOfWork CreateMaster(string masterDatasetKey, string directoryPath);

	/// <summary>
	/// Gets the unit of work for an existing snapshot identified by dataset and revision.
	/// This creates no snapshot file; its lifetime is owned by the factory's dependency scope.
	/// </summary>
	public ISnapshotUnitOfWork CreateSnapshot(string masterDatasetKey, long revision);

	/// <summary>
	/// Gets the candidate.db unit cached beside the Local database for download preparation.
	/// The database file must already exist.
	/// </summary>
	public ICandidateUnitOfWork CreateCandidate();

	/// <summary>
	/// Disposes the cached Local unit and removes it so the next CreateLocal opens a new context.
	/// Does nothing when no Local unit is cached.
	/// </summary>
	public Task CloseLocal();

	/// <summary>
	/// Disposes the cached Candidate unit and removes it before database replacement.
	/// Does nothing when no Candidate unit is cached.
	/// </summary>
	public Task CloseCandidate();

	/// <summary>
	/// Disposes and removes the cached Master unit in its standard working directory.
	/// Does nothing when the unit is not cached.
	/// </summary>
	public Task CloseMaster(string masterDatasetKey);

	/// <summary>
	/// Disposes and removes the cached Master copy in the supplied operation directory.
	/// Does nothing when the unit is not cached.
	/// </summary>
	public Task CloseMaster(string masterDatasetKey, string directoryPath);

	/// <summary>
	/// Disposes and removes the cached snapshot identified by dataset and revision.
	/// Does nothing when the unit is not cached.
	/// </summary>
	public Task CloseSnapshot(string masterDatasetKey, long revision);
}
