namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Contains the cumulative balance of one account at the selected date.
/// Amount is the sum of entry amounts in the account currency.
/// BaseAmount sums individually rounded base amounts using stored entry rates.
/// Only Confirmed transactions contribute to either amount.
/// The selected date includes the complete day in the device timezone.
/// Current catalog rates do not revalue the result.
/// An existing account without qualifying entries has zero balances.
/// This calculated result is not a stored running balance.
/// </summary>
public record AccountBalanceInfo
{
	public Guid AccountId { get; set; }
	public Guid CurrencyId { get; set; }
	public decimal Amount { get; set; }
	public decimal BaseAmount { get; set; }
}
