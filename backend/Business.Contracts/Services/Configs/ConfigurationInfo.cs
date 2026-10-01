using Business.Models.Enums;

namespace Business.Contracts.Services.Configs;

/// <summary>
/// Contains the combined System and Local settings for the current dataset.
/// BaseCurrencyId, BalancingAccountId and precisions come from System configuration.
/// Naming preferences, conflict priority and sync trigger come from Local configuration.
/// Init-only properties keep the returned snapshot read-only after construction.
/// Entity selections are represented only by IDs, never navigation objects.
/// The snapshot is detached from persistent configuration rows.
/// Dataset keys, row identities and synchronization revisions are not settings.
/// Changes are made through Setup services and observed by reading a new snapshot.
/// </summary>
public record ConfigurationInfo
{
	public Guid BaseCurrencyId { get; init; }
	public Guid? BalancingAccountId { get; init; }
	public int AmountPrecision { get; init; }
	public int RatePrecision { get; init; }
	public AccountNameOrder AccountNameOrder { get; init; }
	public required string DefaultAccountNameSeparator { get; init; }
	public ConflictPriority ConflictPriority { get; init; }
	public SyncTrigger SyncTrigger { get; init; }
}
