namespace Synchronization.Contracts.Services.Diagnostics;

/// <summary>
/// Identifies the entity family in a received-change summary.
/// Used by synchronization report rows.
/// Undefined is not a valid counted entity family.
/// Groups and classifications cover their respective catalog types.
/// SystemConfiguration participates in synchronization.
/// Local configuration is deliberately excluded.
/// Counts describe received changes rather than uploaded work.
/// This enum does not select a persistence table or wire payload schema.
/// </summary>
public enum SyncEntityType
{
	Undefined = 0,
	Group = 1,
	Classification = 2,
	Currency = 3,
	Rate = 4,
	Account = 5,
	Transaction = 6,
	Template = 7,
	Report = 8,
	SystemConfiguration = 9
}
