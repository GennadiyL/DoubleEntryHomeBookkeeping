namespace DataAccess.Contracts.Commands;

public interface ICleanDataFilesCommand
{
	/// <summary>
	/// Deletes the specified database file and all associated provider-specific data files.
	/// The caller must close database connections before cleanup. Missing files are ignored.
	/// </summary>
	public void CleanAll(string databaseFilePath);
}
