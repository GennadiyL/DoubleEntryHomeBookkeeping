using Business.Models.Enums;

namespace DataAccess.Contracts.Queries;

/// <summary>
/// Supplies already validated criteria for a transaction database read.
/// UTC boundaries use an inclusive start and exclusive end.
/// Optional account classification criteria are applied to entry accounts.
/// Ids restrict refreshes to an existing displayed selection.
/// MaximumCount limits distinct transactions before loading entries.
/// State restricts accounting reads when supplied.
/// Null criteria impose no additional restriction.
/// The service chooses criteria and interprets empty results.
/// </summary>
public record TransactionSearch
{
	public DateTime? FromDateTime { get; init; }
	public DateTime? BeforeDateTime { get; init; }
	public Guid? AccountId { get; init; }
	public Guid? CategoryId { get; init; }
	public Guid? CorrespondentId { get; init; }
	public Guid? ProjectId { get; init; }
	public List<Guid>? Ids { get; init; }
	public TransactionState? State { get; init; }
	public int? MaximumCount { get; init; }
}
