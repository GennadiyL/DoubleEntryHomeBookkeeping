namespace Dehb.WebApi.Params;

/// <summary>
/// Supplies an inclusive device-local date range for transaction deletion.
/// The Business service validates the range and deletes the entire matching set.
/// </summary>
public record DeleteTransactionsParam
{
	public DateOnly FromDate { get; set; }
	public DateOnly ToDate { get; set; }
}