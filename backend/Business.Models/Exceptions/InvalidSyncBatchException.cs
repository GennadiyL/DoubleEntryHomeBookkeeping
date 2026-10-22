using Business.Core.Entities;

namespace Business.Models.Exceptions;

/// <summary>
/// Indicates that an incoming synchronization batch cannot be safely applied.
/// Includes malformed exchange data and unresolved business constraints.
/// Messages identify the invalid object or required corrective action.
/// Derives from the shared domain exception base for infrastructure handling.
/// No transport status, credentials or provider exception is exposed.
/// A rejected batch must be rolled back by its transaction owner.
/// Rejection does not acknowledge or discard the Local's durable batch.
/// Retry policy and user-facing diagnostics remain caller responsibilities.
/// </summary>
public sealed class InvalidSyncBatchException : BaseException
{
	public InvalidSyncBatchException(string message) : base(message)
	{
	}
}
