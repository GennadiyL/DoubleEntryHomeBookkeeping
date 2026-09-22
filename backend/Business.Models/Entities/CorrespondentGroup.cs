using Business.Models.Entities.Base;

namespace Business.Models.Entities;

/// <summary>
/// Represents a persistent hierarchical group of transaction correspondents.
/// It participates in the correspondent catalog and owns its groups and entries.
/// Its writable identifier can be assigned during creation or materialization.
/// Parameterless construction supports persistence and object initialization.
/// The model stores correspondent-group state without implementing business workflows.
/// </summary>
public class CorrespondentGroup : GroupEntity<CorrespondentGroup, Correspondent>
{
}
