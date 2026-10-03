namespace Dehb.WebApi.Params;

/// <summary>
/// Selects whole transactions by Category and an inclusive date range.
/// Business validates the identity and performs atomic deletion.
/// </summary>
public record DeleteTransactionsByCategoryParam : DeleteTransactionsParam
{
	public Guid CategoryId { get; set; }
}
