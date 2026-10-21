using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

/// <summary>
/// Persists the single Master-owned dataset configuration row.
/// Stores initialization settings and the current committed snapshot revision.
/// Local receives these values through complete database replacement.
/// The row carries no outgoing entity-change tracking flags.
/// The balancing account is identified by the shared fixed constant.
/// BaseCurrencyId remains a foreign key to the configured base currency.
/// Revision changes share the transaction that records accepted batch data.
/// Mapping exposes a detached business configuration without provider details.
/// </summary>
internal class SystemConfig : IDalEntity
{
	public Guid Id { get; set; }
	public Guid BaseCurrencyId { get; set; }
	public string MasterDatasetKey { get; set; } = string.Empty;
	public long CurrentRevision { get; set; }
	public int AmountPrecision { get; set; } = 2;
	public int RatePrecision { get; set; } = 4;
}
