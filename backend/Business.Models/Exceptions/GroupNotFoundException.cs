using Business.Core.Entities;

namespace Business.Models.Exceptions;

/// <summary>
/// Signals that a requested catalog group could not be found.
/// Derives from the common business exception base for domain error handling.
/// Operations raise this exception when they cannot resolve a required entity.
/// The supplied message describes the failed lookup for the consuming error handler.
/// Each instance represents one failure without storing persistent entity state.
/// The exception does not validate data, retry operations, or choose a transport response.
/// </summary>
public class GroupNotFoundException : BaseException
{
	public GroupNotFoundException(string message) : base(message)
	{
	}
}
