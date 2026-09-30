using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent hierarchical group of projects.
/// Parent and child relationships stay within this concrete group family.
/// The elements collection holds the projects assigned directly to the group.
/// Services initialize the fixed root and manage non-root moves, merges, and deletions.
/// Writable inherited identity supports creation and materialization of persistent state.
/// The model carries data; business services implement validation and lifecycle operations.
/// </summary>
public class ProjectGroup : GroupEntity<ProjectGroup, Project>
{
}
