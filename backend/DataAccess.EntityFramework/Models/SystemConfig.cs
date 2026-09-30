using Business.Models.Enums;
using DataAccess.Core.Entities;

namespace DataAccess.EntityFramework.Models;

internal class SystemConfig : IDalEntity
{
	public Guid Id { get; set; }
	public long? EditRevision { get; set; }
	public long? DeleteRevision { get; set; }
	public ModificationType ModificationType { get; set; }
	public Guid BaseCurrencyId { get; set; }
	public Currency? BaseCurrency { get; set; }
	public string MasterDatasetKey { get; set; } = string.Empty;
	public Guid? BalancingAccountId { get; set; }
	public Account? BalancingAccount { get; set; }
	public int AmountPrecision { get; set; } = 2;
	public int RatePrecision { get; set; } = 4;
}
