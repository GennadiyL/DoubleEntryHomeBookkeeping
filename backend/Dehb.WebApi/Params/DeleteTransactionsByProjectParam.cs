namespace Dehb.WebApi.Params;

/// <summary>
/// Selects whole transactions by Project and an inclusive date range.
/// Business validates the identity and performs atomic deletion.
/// </summary>
public record DeleteTransactionsByProjectParam : DeleteTransactionsParam
{
	public Guid ProjectId { get; set; }
}
