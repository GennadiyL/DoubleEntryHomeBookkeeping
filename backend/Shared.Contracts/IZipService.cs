using Shared.Contracts.Zip;

namespace Shared.Contracts;

public interface IZipService
{
	/// <summary>
	/// Performs the Zip operation through the injectable ZIP archive service contract.
	/// </summary>
	public List<string> Zip(string zipFile, IList<string> files, ZipCompressionLevel compressionLevel);

	/// <summary>
	/// Performs the Unzip operation through the injectable ZIP archive service contract.
	/// </summary>
	public List<string> Unzip(string zipFile, string destinationFolder);
}
