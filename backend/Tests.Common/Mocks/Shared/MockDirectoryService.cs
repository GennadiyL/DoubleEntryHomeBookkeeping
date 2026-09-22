using Shared.Contracts;

namespace Tests.Common.Mocks.Shared;

/// <summary>
/// Defines the directory service test double.
/// Provides controllable directory-operation behavior without modifying the real file system.
/// Tests construct or derive from it and override only behavior relevant to a scenario.
/// It implements the complete IDirectoryService surface used by shared consumers.
/// It is test infrastructure and is not registered by production hosts.
/// </summary>
public class MockDirectoryService : IDirectoryService
{
	private const int _addDays = -3;

	private readonly IDateTimeService _dateTimeService;

	public MockDirectoryService(IDateTimeService dateTimeService) => _dateTimeService = dateTimeService;

	public virtual DirectoryInfo CreateDirectory(string path) => CurrentDirectoryInfo();
	public virtual DirectoryInfo CreateTempSubdirectory(string? prefix = null) => CurrentDirectoryInfo();
	public virtual void Move(string sourceDirName, string destDirName) { }
	public virtual void Delete(string path) { }
	public virtual void Delete(string path, bool recursive) { }
	public virtual bool Exists(string path) => false;
	public virtual string[] GetLogicalDrives() => [];
	public virtual DirectoryInfo? GetParent(string path) => null;
	public virtual string GetDirectoryRoot(string path) => string.Empty;
	public virtual string GetCurrentDirectory() => string.Empty;
	public virtual void SetCurrentDirectory(string path) { }
	public virtual void SetCreationTime(string path, DateTime creationTime) { }
	public virtual void SetCreationTimeUtc(string path, DateTime creationTimeUtc) { }
	public virtual DateTime GetCreationTime(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual DateTime GetCreationTimeUtc(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual void SetLastWriteTime(string path, DateTime lastWriteTime) { }
	public virtual void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) { }
	public virtual DateTime GetLastWriteTime(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual DateTime GetLastWriteTimeUtc(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual void SetLastAccessTime(string path, DateTime lastAccessTime) { }
	public virtual void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) { }
	public virtual DateTime GetLastAccessTime(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual DateTime GetLastAccessTimeUtc(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual string[] GetFiles(string path) => [];
	public virtual string[] GetFiles(string path, string searchPattern) => [];
	public virtual string[] GetFiles(string path, string searchPattern, SearchOption searchOption) => [];
	public virtual string[] GetFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => [];
	public virtual string[] GetDirectories(string path) => [];
	public virtual string[] GetDirectories(string path, string searchPattern) => [];
	public virtual string[] GetDirectories(string path, string searchPattern, SearchOption searchOption) => [];
	public virtual string[] GetDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => [];
	public virtual string[] GetFileSystemEntries(string path) => [];
	public virtual string[] GetFileSystemEntries(string path, string searchPattern) => [];
	public virtual string[] GetFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => [];
	public virtual string[] GetFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => [];
	public virtual IEnumerable<string> EnumerateDirectories(string path) => [];
	public virtual IEnumerable<string> EnumerateDirectories(string path, string searchPattern) => [];
	public virtual IEnumerable<string> EnumerateDirectories(string path, string searchPattern, SearchOption searchOption) => [];
	public virtual IEnumerable<string> EnumerateDirectories(string path, string searchPattern, EnumerationOptions enumerationOptions) => [];
	public virtual IEnumerable<string> EnumerateFiles(string path) => [];
	public virtual IEnumerable<string> EnumerateFiles(string path, string searchPattern) => [];
	public virtual IEnumerable<string> EnumerateFiles(string path, string searchPattern, SearchOption searchOption) => [];
	public virtual IEnumerable<string> EnumerateFiles(string path, string searchPattern, EnumerationOptions enumerationOptions) => [];
	public virtual IEnumerable<string> EnumerateFileSystemEntries(string path) => [];
	public virtual IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern) => [];
	public virtual IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, SearchOption searchOption) => [];
	public virtual IEnumerable<string> EnumerateFileSystemEntries(string path, string searchPattern, EnumerationOptions enumerationOptions) => [];

	private static DirectoryInfo CurrentDirectoryInfo() => new(".");
}
