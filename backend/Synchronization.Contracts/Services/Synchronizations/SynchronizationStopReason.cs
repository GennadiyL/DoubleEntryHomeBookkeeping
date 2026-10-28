namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Explains an expected synchronization stop requiring caller attention or retry.
/// Undefined is an unset value rather than a usable explanation.
/// Token reasons accompany AuthorizationRequired outcomes.
/// Availability, transport and acknowledgement reasons accompany Deferred outcomes.
/// TokenExpired requires known expiry rather than an inferred network failure.
/// ConnectionLost does not prove that Master rejected submitted changes.
/// Complete and caller-cancelled outcomes leave their nullable reason empty.
/// Invalid data, access denial and unexpected failures remain exceptions.
/// </summary>
public enum SynchronizationStopReason
{
	Undefined = 0,
	TokenMissing = 1,
	TokenExpired = 2,
	TokenRejected = 3,
	NetworkUnavailable = 4,
	ServerUnavailable = 5,
	MasterBusy = 6,
	ConnectionLost = 7,
	Timeout = 8,
	AcknowledgementNotAccepted = 9
}
