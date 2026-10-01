namespace Business.Contracts.Services.Accounts;

/// <summary>
/// Contains an existing account's complete editable values and reference labels.
/// Declares its own group, name, favorite and currency fields independently of the tree record.
/// Classification identities select the optional category, correspondent and project.
/// Their current names allow the edit dialog to display selections immediately.
/// An absent optional classification has a null identity and name.
/// Reference labels are display values, not commands to rename referenced entities.
/// Reading this detached record does not persist changes or expose synchronization fields.
/// Account Update accepts separate input and preserves the currency fixed by the first save.
/// </summary>
public record AccountInfo //: AccountElementInfo
{
	public Guid Id { get; set; }
	public Guid GroupId { get; set; }
	public string GroupName { get; set; } = string.Empty;
	public string Name { get; set; } = string.Empty;
	public string? Description { get; set; }
	public int Order { get; set; }
	public bool IsFavorite { get; set; }
	public Guid CurrencyId { get; set; }
	public string CurrencyName { get; set; } = string.Empty;
	public Guid? CategoryId { get; set; }
	public string? CategoryName { get; set; }
	public Guid? CorrespondentId { get; set; }
	public string? CorrespondentName { get; set; }
	public Guid? ProjectId { get; set; }
	public string? ProjectName { get; set; }
}
