namespace DataAccess.Contracts;

public interface IMasterUnitOfWork : IAppUnitOfWork
{
	public string MasterDatasetKey { get; }
}
