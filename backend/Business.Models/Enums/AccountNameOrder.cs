namespace Business.Models.Enums;

/// <summary>
/// Defines the six orders of correspondent, category, and project name components.
/// Local configuration stores the choice, defaulting to CorrespondentCategoryProject.
/// The configured separator is applied between slots, including absent components.
/// Services use the choice during account creation or explicit name restoration.
/// Explicit numeric values preserve the meaning of stored or exchanged selections.
/// The enum describes state or preferences without executing the corresponding operations.
/// </summary>
public enum AccountNameOrder
{
	Undefined = 0,
	CorrespondentCategoryProject = 1,
	CorrespondentProjectCategory = 2,
	CategoryCorrespondentProject = 3,
	CategoryProjectCorrespondent = 4,
	ProjectCorrespondentCategory = 5,
	ProjectCategoryCorrespondent = 6
}
