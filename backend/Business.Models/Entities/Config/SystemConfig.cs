using Business.Core.Entities;

namespace Business.Models.Entities.Config;

/// <summary>
/// Stores the single Master-owned configuration row shared with Local snapshots.
/// MasterDatasetKey identifies the dataset independently of the row identity.
/// BaseCurrencyId and both precision settings are immutable after initialization.
/// AmountPrecision and RatePrecision range from zero to four.
/// CurrentRevision identifies the committed Master version represented by this database.
/// Master advances the revision atomically with accepted batches and their receipts.
/// Local reads the downloaded values and never captures this row as a change.
/// The balancing account has a fixed identity in EntitiesIds rather than a setting.
/// </summary>
public class SystemConfig : BaseEntity
{
	public Guid BaseCurrencyId { get; set; }
	public string MasterDatasetKey { get; set; } = string.Empty;
	public long CurrentRevision { get; set; }
	public int AmountPrecision { get; set; } = 2;
	public int RatePrecision { get; set; } = 4;
}
