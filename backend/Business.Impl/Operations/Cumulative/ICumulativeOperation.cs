namespace Business.Impl.Operations.Cumulative;

/// <summary>
/// Coordinates cumulative maintenance within an existing service action.
/// Queries the opening balance for incremental account recalculation.
/// Business substitutes zero when the DAL reports absence.
/// Full recalculation starts from zero without a preceding lookup.
/// The provider command performs the actual cache writes.
/// Calls preserve synchronization metadata on unaffected transactions.
/// The operation neither receives nor manages transaction context.
/// Failures propagate to the service that owns the unit of work.
/// </summary>
internal interface ICumulativeOperation
{
	public Task RecalculateAsync(Guid accountId, CancellationToken cancellationToken = default);
	public Task RecalculateAsync(Guid accountId, DateTime fromDateTime, CancellationToken cancellationToken = default);
}
