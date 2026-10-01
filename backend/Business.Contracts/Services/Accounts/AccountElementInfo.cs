using Business.Contracts.Services.Trees;

namespace Business.Contracts.Services.Accounts;

/// <summary>
/// Describes one account for catalog browsing and selection.
/// GroupId connects the element to its catalog group.
/// Used only for the four-column account tree and selection.
/// Contains values rather than persistent entity references.
/// Identifiers refer to existing bookkeeping records.
/// Does not expose synchronization revisions or modification flags.
/// Reading this record does not save changes.
/// Persistence remains the responsibility of the corresponding mutation operation.
/// </summary>
public record AccountElementInfo : ElementInfo
{
	public Guid CurrencyId { get; set; }
	public string CurrencyName { get; set; } = string.Empty;
}
