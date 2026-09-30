using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents reusable transaction instructions within a template group.
/// Inherits catalog naming, description, favorites, ordering, and synchronization state.
/// Owns account-bearing entries and may be empty or unbalanced.
/// Services apply the template and resolve rates when constructing transaction entries.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class Template : ElementEntity<TemplateGroup, Template>
{
	public List<TemplateEntry> Entries { get; set; } = new();
}
