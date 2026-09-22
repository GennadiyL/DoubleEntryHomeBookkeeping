using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent hierarchical group of bookkeeping projects.
/// It participates in the project catalog and owns child groups and projects.
/// Its writable identifier can be assigned during creation or materialization.
/// Parameterless construction supports persistence and object initialization.
/// The model stores project-group state and does not implement business workflows.
/// </summary>
public class ProjectGroup : GroupEntity<ProjectGroup, Project>
{
}
