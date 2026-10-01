using Business.Contracts.Base.Services;
using Business.Contracts.Services.Trees;
using Business.Models.Entities;

namespace Business.Contracts.Services;

/// <summary>
/// Provides Correspondent classification reads and maintenance.
/// Uses ElementParam for edits and detached ElementInfo for full edit reads.
/// Names are unique within a matching group; favorites and order use separate operations.
/// Accounts may reference this classification; those references prevent ordinary deletion.
/// Combining replaces the matching account references and soft-deletes the source.
/// Changes preserve stored account names and commit with their tracking atomically.
/// </summary>
public interface ICorrespondentService :
	IElementService<CorrespondentGroup, Correspondent>,
	IUpdateEntityService<ElementParam>,
	IReadEntityService<ElementInfo>
{
}
