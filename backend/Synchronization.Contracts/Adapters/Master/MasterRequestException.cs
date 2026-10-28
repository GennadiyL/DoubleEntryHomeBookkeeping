using Business.Core.Entities;
using Synchronization.Contracts.Services.Synchronizations;

namespace Synchronization.Contracts.Adapters.Master;

/// <summary>
/// Reports an expected provider stop through the Master adapter boundary.
/// Azure adapters translate token and transport conditions into a specific reason.
/// The synchronization workflow converts this exception into its application result.
/// Dataset access denial and invalid payloads remain distinct exceptions.
/// Caller cancellation retains the standard cancellation exception contract.
/// Token reasons mean this request was rejected before processing; prior requests may remain uncertain.
/// A transport stop never proves that submitted batches were not committed.
/// The saved container and its original batch identities remain authoritative for retry.
/// Messages must not include credentials or raw authorization tokens.
/// </summary>
public sealed class MasterRequestException : BaseException
{
	public SynchronizationStopReason Reason { get; }

	public MasterRequestException(SynchronizationStopReason reason, string message, Exception? innerException = null)
		: base(message, innerException)
	{
		if (reason is not (SynchronizationStopReason.TokenMissing or SynchronizationStopReason.TokenExpired or
			SynchronizationStopReason.TokenRejected or SynchronizationStopReason.NetworkUnavailable or
			SynchronizationStopReason.ServerUnavailable or SynchronizationStopReason.ConnectionLost or SynchronizationStopReason.Timeout))
		{
			throw new ArgumentOutOfRangeException(nameof(reason));
		}
		Reason = reason;
	}
}
