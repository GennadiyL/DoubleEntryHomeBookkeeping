namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Selects the preferred source of conflicting synchronized content.
/// Master prefers existing Master content.
/// Local prefers submitted Local content.
/// Undefined is an unset value and must be rejected.
/// Business validity takes precedence over this preference.
/// Ordering retains its separate synchronization rules.
/// This enum belongs exclusively to the Master service contract.
/// Adapters explicitly map their independent priority values.
/// </summary>
public enum ConflictPriority
{
	Undefined = 0,
	Master = 1,
	Local = 2
}
