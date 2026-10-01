using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents an account used by transaction and template entries.
/// Belongs to one account group and references one currency.
/// Optional category, correspondent and project references classify account activity.
/// The first successful save fixes currency permanently, even before entry use.
/// Account names may be duplicated and do not change automatically with classifications.
/// Any transaction or template reference prevents ordinary deletion.
/// Replacement requires the same currency and preserves entry amounts, rates and order.
/// Deleting the selected balancing account clears the System selection.
/// Services validate and persist the lifecycle; the model carries persistent data.
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
