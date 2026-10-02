using Business.Core.Entities;

namespace Business.Models.Exceptions;

/// <summary>
/// Signals an invalid currency maintenance request.
/// Covers invalid metadata, rates, ordering and protected deletion.
/// Distinguishes rejected input from a missing currency identity.
/// The service provides a message describing the failed rule.
/// The exception contains no persistent currency state.
/// It does not modify data or synchronization metadata.
/// The Web API maps this expected failure to a bad-request response.
/// Persistence and retry decisions remain outside this exception.
/// </summary>
public class InvalidCurrencyException : BaseException
{
	public InvalidCurrencyException(string message) : base(message)
	{
	}
}
