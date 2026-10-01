using Business.Core.Entities;

namespace Business.Models.Entities;

/// <summary>
/// Represents an account-bearing entry owned by a transaction template.
/// Parent and account references match their corresponding foreign keys.
/// Amount is expressed in the account currency using configured amount precision.
/// Position is the zero-based entry index within the complete template.
/// Rates are resolved when applying the template and are not stored on this entry.
/// Aggregate updates replace all entry rows with new identities in submitted list order.
/// Entries have no independent synchronization flags; changes mark the parent content.
/// Entry lifecycle and persistence are managed at the template aggregate boundary.
/// </summary>
public class TemplateEntry : BaseEntity
{
	public required Template Template { get; set; }
	public Guid TemplateId { get; set; }
	public required Account Account { get; set; }
	public Guid AccountId { get; set; }
	public decimal Amount { get; set; }
	public int Position { get; set; }
}
