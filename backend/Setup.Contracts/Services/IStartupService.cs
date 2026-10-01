using Setup.Contracts.Services.Startups;

namespace Setup.Contracts.Services;

/// <summary>
/// Provides startup state, initial creation and existing-dataset setup.
/// These are logical API/BFF operations, not selected HTTP routes.
/// Local startup inspection must work offline.
/// Cloud setup requires owner authentication and internet access.
/// Initial content includes roots, base currency and rebalancing configuration.
/// Creation requires confirmed absence of the Master dataset.
/// Successful cloud creation survives a subsequent download failure.
/// Usable local installation remains necessary for normal bookkeeping.
/// </summary>
public interface IStartupService
{
	/// <summary>
	/// Reads local existence, recovery and expiry state for startup navigation; does not mutate business data.
	/// </summary>
	public Task<OperationStatusInfo> GetStartupState(CancellationToken cancellationToken = default);

	/// <summary>
	/// Checks Master availability for the Create screen; a failed check must never mean absence.
	/// </summary>
	public Task<OperationStatusInfo> CheckMasterExists(CancellationToken cancellationToken = default);

	/// <summary>
	/// Creates owner books and registers a local copy for initial download; validates immutable creation choices.
	/// </summary>
	public Task<SetupInfo> CreateBooks(CreateBooks command, CancellationToken cancellationToken = default);

	/// <summary>
	/// Authenticates the owner and registers a local copy for the setup download; retries remain explicit.
	/// </summary>
	public Task<SetupInfo> OpenBooks(OpenBooks command, CancellationToken cancellationToken = default);
}
