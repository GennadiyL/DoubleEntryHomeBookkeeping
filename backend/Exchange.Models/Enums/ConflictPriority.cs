namespace Exchange.Models.Enums;

/// <summary>
/// Captures the content-conflict preference of an outgoing batch.
/// Undefined represents a missing selection and is not a valid preference.
/// Master prefers the existing Master content when a conflict requires priority.
/// Local prefers the incoming Local content under the same conditions.
/// Business validity takes precedence over the selected preference.
/// The same-Local exception and separate ordering rules remain applicable.
/// Explicit numeric values are serialized independently of domain enums.
/// Provider and domain boundaries map their own enum values explicitly.
/// </summary>
public enum ConflictPriority
{
	Undefined = 0,
	Master = 1,
	Local = 2
}
