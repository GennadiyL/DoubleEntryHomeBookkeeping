namespace Business.Contracts.Services.Transactions;

/// <summary>
/// Identifies the current displayed selection for a post-edit refresh.
/// Date is the inclusive end date in the device timezone.
/// TransactionIds preserve membership instead of refilling vacant places.
/// At most one account or classification filter may be supplied.
/// The identifiers describe read criteria rather than entity edits.
/// The service validates referenced filter identities.
/// Complete current transaction values are requested for retained rows.
/// Transaction ownership and database details are not exposed.
/// </summary>
public record TransactionRefreshParam
{
	public DateOnly Date { get; set; }
	public List<Guid> TransactionIds { get; } = new();
	public Guid? AccountId { get; set; }
	public Guid? CategoryId { get; set; }
	public Guid? CorrespondentId { get; set; }
	public Guid? ProjectId { get; set; }
}
