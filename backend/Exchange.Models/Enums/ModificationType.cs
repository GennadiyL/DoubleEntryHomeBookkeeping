namespace Exchange.Models.Enums;

/// <summary>
/// Describes the change semantics captured for one outgoing object.
/// None is the initial value for creation; the Added collection identifies creation.
/// Content includes business content changes and deletion semantics.
/// Order identifies independent catalog ordering changes.
/// Content and Order may be combined without losing either meaning.
/// Order alone never authorizes overwriting business content.
/// Transaction and template entry positions belong to parent content.
/// Explicit numeric values are independent of domain enum declarations.
/// </summary>
[Flags]
public enum ModificationType
{
	None = 0,
	Content = 1,
	Order = 2
}
