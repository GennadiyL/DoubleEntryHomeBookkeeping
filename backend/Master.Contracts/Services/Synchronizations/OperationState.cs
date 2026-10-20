namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Describes the server-observable outcome of a synchronization attempt.
/// Undefined is an unset result and never indicates success.
/// Waiting and Running describe work that has not finished.
/// OutcomeUnknown requires durable attempt resolution.
/// Published means the Master snapshot is available for download.
/// Complete means the applicable completion conditions are satisfied.
/// Expired and Failed do not indicate a successful synchronization.
/// Local startup and installation UI stages are outside this contract.
/// </summary>
public enum OperationState
{
	Undefined = 0,
	Waiting = 5,
	Running = 6,
	OutcomeUnknown = 7,
	Published = 8,
	Complete = 10,
	Expired = 11,
	Failed = 12
}
