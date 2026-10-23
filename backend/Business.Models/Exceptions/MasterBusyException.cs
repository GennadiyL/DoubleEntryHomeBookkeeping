using Business.Core.Entities;

namespace Business.Models.Exceptions;

/// <summary>
/// Indicates that another operation currently owns exclusive Master access.
/// Synchronization raises this before opening a database transaction.
/// The rejected call does not apply batches or create receipts.
/// The caller retains pending changes and may retry later with the same batch keys.
/// Automatic retries and waiting are outside this business exception.
/// Infrastructure decides how to present the busy state to the user.
/// Storage failures and cancellation remain distinct from ordinary contention.
/// No provider-specific lease identifier or transport status is exposed.
/// </summary>
public sealed class MasterBusyException : BaseException
{
	public MasterBusyException() : base("The Master database is busy. Try synchronizing again later.")
	{
	}
}
