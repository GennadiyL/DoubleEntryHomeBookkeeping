namespace Business.Models.Enums;

/// <summary>
/// Describes the outcome of a Master authorization preflight.
/// LoginRequired is the safe default when authorization is unavailable.
/// Missing, expired and rejected tokens all require the same login flow.
/// Authorized allows the synchronization workflow to proceed.
/// The trusted in-process adapter returns Authorized without checking a token.
/// Network and server failures remain exceptions rather than login outcomes.
/// Caller cancellation retains the standard cancellation exception contract.
/// Protected requests must still enforce authorization independently.
/// </summary>
public enum AuthorizationResult
{
	LoginRequired = 0,
	Authorized = 1
}
