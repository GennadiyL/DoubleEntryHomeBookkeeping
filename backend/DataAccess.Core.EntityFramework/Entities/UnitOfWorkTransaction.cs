using DataAccess.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace DataAccess.Core.EntityFramework.Entities;

/// <summary>
/// Defines the Entity Framework transaction wrapper.
/// Associates an Entity Framework transaction with the DbContext that created it.
/// UnitOfWork creates, validates, commits, rolls back, and disposes each wrapper.
/// It is an internal implementation of the public IUnitOfWorkTransaction abstraction.
/// It is not exposed as a concrete dependency outside this assembly.
/// </summary>
internal sealed class UnitOfWorkTransaction : IUnitOfWorkTransaction
{
	private readonly DbContext _context;
	private readonly IDbContextTransaction _transaction;

	public UnitOfWorkTransaction(DbContext context, IDbContextTransaction transaction)
	{
		_context = context ?? throw new ArgumentNullException(nameof(context));
		_transaction = transaction ?? throw new ArgumentNullException(nameof(transaction));
	}

	public bool BelongsTo(DbContext context) => ReferenceEquals(_context, context);

	public Task CommitAsync(CancellationToken cancellationToken) => _transaction.CommitAsync(cancellationToken);

	public Task RollbackAsync(CancellationToken cancellationToken) => _transaction.RollbackAsync(cancellationToken);

	public ValueTask DisposeAsync() => _transaction.DisposeAsync();
}
