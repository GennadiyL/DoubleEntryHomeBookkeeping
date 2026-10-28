namespace DataAccess.Contracts;

public interface ISnapshotUnitOfWork : IAppUnitOfWork
{
	public string MasterDatasetKey { get; }
	public long Revision { get; }
}
