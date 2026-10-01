using Business.Core.Entities;

namespace Business.Models.Exceptions;

/// <summary>
/// Signals that a requested group is missing or already deleted.
/// Used when a required live target cannot be returned or mutated.
/// GetById does not return a null or empty record for this condition.
/// Update and Delete do not treat a missing target as a successful mutation.
/// Derives from the common business exception base for domain error handling.
/// The supplied message describes the failed lookup.
/// The exception carries no persistent entity state and performs no retries.
/// Transport mapping remains the responsibility of the consuming adapter.
/// </summary>
public class GroupNotFoundException : BaseException
{
	public GroupNotFoundException(string message) : base(message)
	{
	}
}
