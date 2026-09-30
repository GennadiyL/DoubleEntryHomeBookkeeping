namespace Business.Models.Enums;

/// <summary>
/// Identifies the preferred source for conflicting synchronized content.
/// Master prefers master content; Local prefers incoming local content.
/// Undefined represents an unset choice, while Local configuration defaults to Local.
/// Business validity and the same-Local exception govern accepted content; ordering follows separate rules.
/// Explicit numeric values preserve the meaning of stored or exchanged selections.
/// The enum describes state or preferences without executing the corresponding operations.
/// </summary>
public enum ConflictPriority
{
	Undefined = 0,
	Master = 1,
	Local = 2
}
