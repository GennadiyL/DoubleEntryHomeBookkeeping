namespace DataAccess.Core.Entities;

public interface IUnitOfWork
{
	public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default);

	public Task CommitTransactionAsync(
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken = default);

	public Task RollbackTransactionAsync(
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken = default);

	public Task SaveChangesAsync();

	public void SaveChanges();
}
