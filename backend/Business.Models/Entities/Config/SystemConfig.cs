using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities.Config;

/// <summary>
/// Stores the single synchronized configuration row for a Master dataset and its Local copies.
/// Master generates the row identity once; every Local retains it through synchronization.
/// MasterDatasetKey identifies the dataset independently of the row identity.
/// BaseCurrencyId and both precision settings are immutable after initialization.
/// AmountPrecision and RatePrecision range from zero to four, defaulting to two and four.
/// BalancingAccountId optionally selects a base-currency account for assisted balancing.
/// Deletion of that account clears the selection; the setting alone does not protect deletion.
/// Entity references are ID-only and are resolved and validated by services.
/// Mutable setting changes use the shared content-tracking and atomic persistence rules.
/// </summary>
public class SystemConfig : BaseEntity, ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public Guid BaseCurrencyId { get; set; }
	public string MasterDatasetKey { get; set; } = string.Empty;
	public Guid? BalancingAccountId { get; set; }
	public int AmountPrecision { get; set; } = 2;
	public int RatePrecision { get; set; } = 4;
}
