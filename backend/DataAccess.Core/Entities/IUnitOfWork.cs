namespace DataAccess.Core.Entities;

public interface IUnitOfWork
{
	public Task<IUnitOfWorkTransaction> BeginTransaction(CancellationToken cancellationToken = default);

	public Task CommitTransaction(
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken = default);

	public Task RollbackTransaction(
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken = default);

	public Task SaveChanges(CancellationToken cancellationToken = default);

	public void SaveChangesSync();
}
