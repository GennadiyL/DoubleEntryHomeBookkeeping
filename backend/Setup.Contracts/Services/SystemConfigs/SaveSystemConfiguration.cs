namespace Setup.Contracts.Services.SystemConfigs;

/// <summary>
/// Contains editable settings for the single System configuration.
/// Used by SaveConfiguration without an identity parameter.
/// The existing persistent configuration identity is retained.
/// Changes synchronize; base currency and precisions cannot be supplied here.
/// Dataset keys and synchronization bookkeeping are excluded.
/// The service validates settings before changing stored values.
/// A successful state-changing save commits once through AcceptChanges.
/// Invalid settings do not persist partial changes.
/// </summary>
public record SaveSystemConfiguration
{
	public Guid? BalancingAccountId { get; set; }
}
