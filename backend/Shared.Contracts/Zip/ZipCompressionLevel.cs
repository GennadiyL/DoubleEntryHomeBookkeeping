namespace Shared.Contracts.Zip;

/// <summary>
/// Defines the ZIP compression level.
/// Defines transport-neutral compression choices exposed by IZipService.
/// Callers select a value without depending on a concrete compression library.
/// The implementation maps these values to the underlying platform compression settings.
/// It does not perform compression or represent archive formats.
/// </summary>
public enum ZipCompressionLevel
{
	None = 0,
	BestSpeed = 1,
	Default = 6,
	BestCompression = 9
}
