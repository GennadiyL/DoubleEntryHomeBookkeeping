using Business.Core.Entities;
using Business.Models.Entities.Interfaces;
using Business.Models.Enums;

namespace Business.Models.Entities.Config;

public class SystemConfig : BaseEntity, ITrackedEntity
{
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public Guid BaseCurrencyId { get; set; }
	public Currency BaseCurrency { get; set; } = null!;
	public string MasterDatasetKey { get; set; } = string.Empty;
	public Guid? BalancingAccountId { get; set; }
	public Account? BalancingAccount { get; set; }
	public int AmountPrecision { get; set; } = 2;
	public int RatePrecision { get; set; } = 4;
}
