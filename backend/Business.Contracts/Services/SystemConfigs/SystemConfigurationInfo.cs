namespace Business.Contracts.Services.SystemConfigs;

/// <summary>
/// Contains settings from the single System configuration.
/// Used to populate the configuration screen.
/// No configuration identity is needed to select the singleton.
/// Base currency and precisions are read-only after creation.
/// Dataset keys and synchronization revisions are not settings fields.
/// The result does not expose persistent navigation objects.
/// Reading settings does not commit changes.
/// SaveConfiguration persists editable fields separately.
/// </summary>
public record SystemConfigurationInfo
{
	public Guid BaseCurrencyId { get; set; }
	public int AmountPrecision { get; set; }
	public int RatePrecision { get; set; }
	public Guid? BalancingAccountId { get; set; }
}
