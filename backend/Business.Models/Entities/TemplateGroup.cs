using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent hierarchical group of transaction templates.
/// It participates in the template catalog and owns child groups and templates.
/// Its writable identifier can be assigned during creation or materialization.
/// Parameterless construction supports persistence and object initialization.
/// The model stores template-group state and does not implement business workflows.
/// </summary>
public class TemplateGroup : GroupEntity<TemplateGroup, Template>
{
}
