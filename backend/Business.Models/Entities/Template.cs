using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents reusable transaction instructions within a template group.
/// Inherits catalog naming, description, favorites, order and synchronization metadata.
/// Description is optional, independent of Name and copied when applying the template.
/// Owns ordered account-bearing entries and may be empty or unbalanced.
/// Updates replace the complete entry set while preserving template identity.
/// Entry changes participate in the template content synchronization lifecycle.
/// Applying resolves rates and prepares an unsaved transaction without modifying the template.
/// Combining template elements is unsupported; combining template groups remains supported.
/// </summary>
public class Template : ElementEntity<TemplateGroup, Template>
{
	public List<TemplateEntry> Entries { get; set; } = new();
}
