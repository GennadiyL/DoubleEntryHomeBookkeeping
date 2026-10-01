namespace Setup.Contracts.Services.Startups;

/// <summary>
/// Supplies owner credentials for opening an existing Master dataset.
/// Used when installing or reinstalling a complete local copy.
/// Login and Password are required and must not be logged.
/// LocalDatasetKey identifies the new local registration.
/// RequestId identifies a setup attempt across retries.
/// Base currency and precisions are obtained from the existing dataset.
/// Owner authentication must precede registration and download.
/// This command does not permit ordinary business access before installation.
/// </summary>
public record OpenBooks
{
	public required string Login { get; set; }
	public required string Password { get; set; }
	public required string LocalDatasetKey { get; set; }
	public Guid RequestId { get; set; }
}
