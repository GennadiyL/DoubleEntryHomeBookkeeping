namespace DataAccess.Contracts.Commands;

/// <summary>
/// Writes derived account cumulative amounts through the selected provider.
/// Full rebuilding starts from zero across the account history.
/// Range rebuilding consumes a supplied opening balance.
/// The caller determines the account and inclusive UTC boundary.
/// Commands participate in the service-owned unit of work.
/// They do not manage transaction lifetime or synchronization flags.
/// Values use account-currency decimal units at this boundary.
/// Provider conversion and execution failures propagate to the service.
/// </summary>
public interface ICumulativeAmountCommand
{
	public Task Recalculate(Guid accountId, CancellationToken cancellationToken = default);
	public Task Recalculate(Guid accountId, DateTime fromDateTime, decimal initialAmount, CancellationToken cancellationToken = default);
}
