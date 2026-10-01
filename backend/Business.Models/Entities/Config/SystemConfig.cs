using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities.Config;

/// <summary>
/// Stores synchronized settings shared by a master dataset and its local copies.
/// MasterDatasetKey identifies the dataset independently of this configuration row.
/// Base currency and precision choices are initialized once and preserved by services.
/// The optional balancing-account ID selects the account for assisted balancing.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
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
