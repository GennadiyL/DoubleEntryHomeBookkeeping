using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

/// <summary>
/// Provides Project group reads and maintenance for its catalog hierarchy.
/// Composes group editor operations, full group reads, ordering and favorite selection.
/// Uses the shared GroupParam and GroupInfo contracts for non-root editing.
/// The fixed root is included in reads and protected from mutation.
/// GetTree supplies common element columns without specialized aggregate detail.
/// State-changing actions save all affected rows and tracking atomically.
/// </summary>
public interface IProjectGroupService :
	IGroupService<ProjectGroup, Project>,
	IUpdateEntityService<GroupParam>,
	IReadEntityService<GroupInfo>
{
}
