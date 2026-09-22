namespace Shared.Contracts;

public interface IDirectoryService
{
	/// <summary>
	/// Performs the CreateDirectory operation through the injectable directory-system service contract.
	/// </summary>
	public DirectoryInfo CreateDirectory(string path);

	/// <summary>
	/// Performs the CreateTempSubdirectory operation through the injectable directory-system service contract.
	/// </summary>
	public DirectoryInfo CreateTempSubdirectory(string? prefix = null);

	/// <summary>
	/// Performs the Move operation through the injectable directory-system service contract.
	/// </summary>
	public void Move(string sourceDirName, string destDirName);

	/// <summary>
	/// Performs the Delete operation through the injectable directory-system service contract.
	/// </summary>
	public void Delete(string path);

	/// <summary>
	/// Performs the Delete operation through the injectable directory-system service contract.
	/// </summary>
	public void Delete(string path, bool recursive);

	/// <summary>
	/// Performs the Exists operation through the injectable directory-system service contract.
	/// </summary>
	public bool Exists(string path);



	/// <summary>
	/// Performs the GetLogicalDrives operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetLogicalDrives();

	/// <summary>
	/// Performs the GetParent operation through the injectable directory-system service contract.
	/// </summary>
	public DirectoryInfo? GetParent(string path);

	/// <summary>
	/// Performs the GetDirectoryRoot operation through the injectable directory-system service contract.
	/// </summary>
	public string GetDirectoryRoot(string path);

	/// <summary>
	/// Performs the GetCurrentDirectory operation through the injectable directory-system service contract.
	/// </summary>
	public string GetCurrentDirectory();

	/// <summary>
	/// Performs the SetCurrentDirectory operation through the injectable directory-system service contract.
	/// </summary>
	public void SetCurrentDirectory(string path);



	/// <summary>
	/// Performs the SetCreationTime operation through the injectable directory-system service contract.
	/// </summary>
	public void SetCreationTime(string path, DateTime creationTime);

	/// <summary>
	/// Performs the SetCreationTimeUtc operation through the injectable directory-system service contract.
	/// </summary>
	public void SetCreationTimeUtc(string path, DateTime creationTimeUtc);

	/// <summary>
	/// Performs the GetCreationTime operation through the injectable directory-system service contract.
	/// </summary>
	public DateTime GetCreationTime(string path);

	/// <summary>
	/// Performs the GetCreationTimeUtc operation through the injectable directory-system service contract.
	/// </summary>
	public DateTime GetCreationTimeUtc(string path);

	/// <summary>
	/// Performs the SetLastWriteTime operation through the injectable directory-system service contract.
	/// </summary>
	public void SetLastWriteTime(string path, DateTime lastWriteTime);

	/// <summary>
	/// Performs the SetLastWriteTimeUtc operation through the injectable directory-system service contract.
	/// </summary>
	public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);

	/// <summary>
	/// Performs the GetLastWriteTime operation through the injectable directory-system service contract.
	/// </summary>
	public DateTime GetLastWriteTime(string path);

	/// <summary>
	/// Performs the GetLastWriteTimeUtc operation through the injectable directory-system service contract.
	/// </summary>
	public DateTime GetLastWriteTimeUtc(string path);

	/// <summary>
	/// Performs the SetLastAccessTime operation through the injectable directory-system service contract.
	/// </summary>
	public void SetLastAccessTime(string path, DateTime lastAccessTime);

	/// <summary>
	/// Performs the SetLastAccessTimeUtc operation through the injectable directory-system service contract.
	/// </summary>
	public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);

	/// <summary>
	/// Performs the GetLastAccessTime operation through the injectable directory-system service contract.
	/// </summary>
	public DateTime GetLastAccessTime(string path);

	/// <summary>
	/// Performs the GetLastAccessTimeUtc operation through the injectable directory-system service contract.
	/// </summary>
	public DateTime GetLastAccessTimeUtc(string path);



	/// <summary>
	/// Performs the GetFiles operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFiles(string path);

	/// <summary>
	/// Performs the GetFiles operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFiles(string path, string searchPattern);

	/// <summary>
	/// Performs the GetFiles operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFiles(string path, string searchPattern, SearchOption searchOption);

	/// <summary>
	/// Performs the GetFiles operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions);

	/// <summary>
	/// Performs the GetDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetDirectories(string path);

	/// <summary>
	/// Performs the GetDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetDirectories(string path, string searchPattern);

	/// <summary>
	/// Performs the GetDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetDirectories(string path, string searchPattern, SearchOption searchOption);

	/// <summary>
	/// Performs the GetDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions);

	/// <summary>
	/// Performs the GetFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFileSystemEntries(string path);

	/// <summary>
	/// Performs the GetFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFileSystemEntries(string path, string searchPattern);

	/// <summary>
	/// Performs the GetFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption);

	/// <summary>
	/// Performs the GetFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions);



	/// <summary>
	/// Performs the EnumerateDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateDirectories(string path);

	/// <summary>
	/// Performs the EnumerateDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern);

	/// <summary>
	/// Performs the EnumerateDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption);

	/// <summary>
	/// Performs the EnumerateDirectories operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions);

	/// <summary>
	/// Performs the EnumerateFiles operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFiles(string path);

	/// <summary>
	/// Performs the EnumerateFiles operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern);

	/// <summary>
	/// Performs the EnumerateFiles operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption);

	/// <summary>
	/// Performs the EnumerateFiles operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions);

	/// <summary>
	/// Performs the EnumerateFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFileSystemEntries(string path);

	/// <summary>
	/// Performs the EnumerateFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern);

	/// <summary>
	/// Performs the EnumerateFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption);

	/// <summary>
	/// Performs the EnumerateFileSystemEntries operation through the injectable directory-system service contract.
	/// </summary>
	public IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions);
}
