using Business.Contracts.Services.Groups;

namespace Business.Contracts.Services.Accounts;

/// <summary>
/// Describes one account for catalog browsing and selection.
/// GroupId connects the element to its catalog group.
/// Created by the service for presentation and editing.
/// Contains values rather than persistent entity references.
/// Identifiers refer to existing bookkeeping records.
/// Does not expose synchronization revisions or modification flags.
/// Reading this record does not save changes.
/// Persistence remains the responsibility of the corresponding mutation operation.
/// </summary>
public record AccountInfo : ElementInfo
{
	public Guid CurrencyId { get; set; }
	public string CurrencyName { get; set; } = string.Empty;
}
