using DataAccess.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Core.EntityFramework.Entities;

/// <summary>
/// Defines the Entity Framework unit-of-work base.
/// Coordinates saves and database transaction lifecycles for a DbContext.
/// Application-specific units of work derive from it and expose repository contracts.
/// It wraps Entity Framework transactions behind IUnitOfWork and IUnitOfWorkTransaction.
/// It does not define domain repositories or provider-specific configuration.
/// </summary>
public abstract class UnitOfWork<TContext> : IUnitOfWork
	where TContext : DbContext
{
	protected TContext Context { get; }

	protected Lazy<IServiceProvider> ServiceProvider { get; }

	protected UnitOfWork(TContext context, Lazy<IServiceProvider> serviceProvider)
	{
		Context = context;
		ServiceProvider = serviceProvider;
	}

	public async Task<IUnitOfWorkTransaction> BeginTransaction(CancellationToken cancellationToken = default)
	{
		IDbContextTransaction transaction =
			await Context.Database.BeginTransactionAsync(cancellationToken);

		return new UnitOfWorkTransaction(Context, transaction);
	}

	public async Task CommitTransaction(
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken = default)
	{
		UnitOfWorkTransaction unitOfWorkTransaction = GetTransaction(transaction);

		await unitOfWorkTransaction.Commit(cancellationToken);
		await unitOfWorkTransaction.DisposeAsync();
	}

	public async Task RollbackTransaction(
		IUnitOfWorkTransaction transaction,
		CancellationToken cancellationToken = default)
	{
		UnitOfWorkTransaction unitOfWorkTransaction = GetTransaction(transaction);

		try
		{
			await unitOfWorkTransaction.Rollback(cancellationToken);
		}
		finally
		{
			try
			{
				await unitOfWorkTransaction.DisposeAsync();
			}
			finally
			{
				await Context.DisposeAsync();
			}
		}
	}

	public void SaveChangesSync() => Context.SaveChanges();

	public Task SaveChanges(CancellationToken cancellationToken = default) => Context.SaveChangesAsync(cancellationToken);

	private UnitOfWorkTransaction GetTransaction(IUnitOfWorkTransaction transaction)
	{
		if (transaction is not UnitOfWorkTransaction unitOfWorkTransaction || !unitOfWorkTransaction.BelongsTo(Context))
		{
			throw new ArgumentException("The transaction does not belong to this unit of work.", nameof(transaction));
		}

		return unitOfWorkTransaction;
	}
}
