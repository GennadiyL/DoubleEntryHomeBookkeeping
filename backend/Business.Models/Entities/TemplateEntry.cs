using Business.Core.Entities;

namespace Business.Models.Entities;

/// <summary>
/// Represents an account-bearing entry owned by a transaction template.
/// Stores parent and account references with their matching foreign keys.
/// Amount and Position preserve reusable entry content and order.
/// Rates are resolved when applying the template; synchronization follows the parent lifecycle.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
