namespace Business.Models.Enums;

/// <summary>
/// Selects when a local copy initiates automatic synchronization.
/// ManualOnly is the default and disables automatic starts.
/// OnStart requests synchronization at application start; OnExit requests it at application exit.
/// Manual synchronization remains available in every mode; automatic starts require Wi-Fi.
/// Explicit numeric values preserve the meaning of stored or exchanged selections.
/// The enum describes state or preferences without executing the corresponding operations.
/// </summary>
public enum SyncTrigger
{
	ManualOnly = 0,
	OnStart = 1,
	OnExit = 2 }
