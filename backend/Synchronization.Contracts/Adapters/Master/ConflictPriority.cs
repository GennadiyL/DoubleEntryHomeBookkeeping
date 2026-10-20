namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Selects the preferred source of conflicting synchronized content.
/// Master prefers existing Master content.
/// Local prefers submitted Local content.
/// Undefined is an unset value and must be rejected.
/// Business validity takes precedence over this preference.
/// Ordering retains its separate synchronization rules.
/// This enum belongs exclusively to the provider-independent adapter contract.
/// Provider implementations map this value to their own priority contract.
/// </summary>
public enum ConflictPriority
{
	Undefined = 0,
	Master = 1,
	Local = 2
}
