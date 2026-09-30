using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents an account used by transaction and template entries.
/// Belongs to an account group and references one currency.
/// Optional category, correspondent, and project references classify account activity.
/// Services preserve the first saved currency and enforce reference-based deletion rules.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class Account : ElementEntity<AccountGroup, Account>
{
	public Currency Currency { get; set; } = null!;
	public Guid CurrencyId { get; set; }
	public Category? Category { get; set; }
	public Guid? CategoryId { get; set; }
	public Correspondent? Correspondent { get; set; }
	public Guid? CorrespondentId { get; set; }
	public Project? Project { get; set; }
	public Guid? ProjectId { get; set; }
}
