namespace Setup.Contracts.Services.Startups;

/// <summary>
/// Identifies the visible stage of setup or synchronization.
/// Undefined represents a missing result and is not success.
/// CreatingMode and OpenMode describe local startup.
/// MasterAbsent and MasterPresent describe a successful existence check.
/// Waiting and Running describe an active attempt.
/// OutcomeUnknown requires durable outcome resolution.
/// Published and Installed precede acknowledged completion.
/// Expired and Failed must not be treated as successful synchronization.
/// </summary>
public enum OperationState
{
	Undefined = 0,
	CreatingMode = 1,
	OpenMode = 2,
	MasterAbsent = 3,
	MasterPresent = 4,
	Waiting = 5,
	Running = 6,
	OutcomeUnknown = 7,
	Published = 8,
	Installed = 9,
	Complete = 10,
	Expired = 11,
	Failed = 12
}
