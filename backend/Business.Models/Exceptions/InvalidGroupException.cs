using Business.Core.Entities;

namespace Business.Models.Exceptions;

/// <summary>
/// Signals that a catalog group operation violates a business rule.
/// Derives from the common business exception base for domain error handling.
/// The caller validates the operation and supplies a message describing the failure.
/// This distinguishes invalid operations from failures to locate an entity.
/// Each instance represents one failure without storing persistent entity state.
/// The exception does not validate data, retry operations, or choose a transport response.
/// </summary>
public class InvalidGroupException : BaseException
{
	public InvalidGroupException(string message) : base(message)
	{
	}
}
