namespace Shared.Contracts.Config;

public interface IConfigurationService
{
	/// <summary>
	/// Gets the scope-owned temporary directory, creating it when needed.
	/// Repeated calls return the same path; disposal removes the directory and its contents.
	/// </summary>
	public string GetTempDirectory();
}
