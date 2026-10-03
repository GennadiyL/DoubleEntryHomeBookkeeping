namespace Dehb.WebApi.Params;

/// <summary>
/// Selects whole transactions by Correspondent and an inclusive date range.
/// Business validates the identity and performs atomic deletion.
/// </summary>
public record DeleteTransactionsByCorrespondentParam : DeleteTransactionsParam
{
	public Guid CorrespondentId { get; set; }
}
