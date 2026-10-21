namespace Synchronization.Contracts.Adapters.TokenStore;

/// <summary>
/// Identifies one protected synchronization token independently of provider DTOs.
/// MasterAddress is the configured absolute server base address.
/// MasterDatasetKey identifies the expected business dataset on that server.
/// LocalDatasetKey identifies the registered Local copy within the dataset.
/// Changing any scope component selects a different stored token.
/// Scope identifiers never authorize access without a valid server-issued token.
/// Storage implementations must not use these values directly as file paths.
/// The scope contains no password or token and is not synchronized business data.
/// </summary>
public record TokenStoreScope
{
	public required Uri MasterAddress { get; set; }
	public required string MasterDatasetKey { get; set; }
	public required string LocalDatasetKey { get; set; }
}
