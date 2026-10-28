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
	/// Gets the unit of work for an existing snapshot identified by dataset and revision.
	/// This creates no snapshot file; its lifetime is owned by the factory's dependency scope.
	/// </summary>
	public ISnapshotUnitOfWork CreateSnapshot(string masterDatasetKey, long revision);
}
