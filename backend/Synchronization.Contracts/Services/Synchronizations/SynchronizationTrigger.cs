namespace Synchronization.Contracts.Services.Synchronizations;

/// <summary>
/// Describes why a synchronization attempt starts.
/// This differs from the stored automatic synchronization preference.
/// Manual is an explicit user action.
/// OnStart and OnExit identify automatic lifecycle actions.
/// Recovery resumes resolution of an earlier attempt.
/// Undefined is unset and must not start an operation.
/// Automatic attempts remain subject to the Wi-Fi requirement.
/// The value does not itself establish authorization or network availability.
/// </summary>
public enum SynchronizationTrigger
{
	Undefined = 0,
	Manual = 1,
	OnStart = 2,
	OnExit = 3,
	Recovery = 4
}
