namespace Dehb.WebApi.Params;

/// <summary>
/// Selects whole transactions by Account and an inclusive date range.
/// Business validates the identity and performs atomic deletion.
/// </summary>
public record DeleteTransactionsByAccountParam : DeleteTransactionsParam
{
	public Guid AccountId { get; set; }
}
