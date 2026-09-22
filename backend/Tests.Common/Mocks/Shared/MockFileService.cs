using System.Text;
using Shared.Contracts;
#pragma warning disable IDE0390

namespace Tests.Common.Mocks.Shared;

/// <summary>
/// Defines the file service test double.
/// Provides controllable file-operation behavior without modifying real files.
/// Tests construct or derive from it and override only behavior relevant to a scenario.
/// It implements the complete IFileService surface used by shared consumers.
/// It is test infrastructure and is not registered by production hosts.
/// </summary>
public class MockFileService : IFileService
{
	private const int _addDays = -3;

	private readonly IDateTimeService _dateTimeService;

	public MockFileService(IDateTimeService dateTimeService) => _dateTimeService = dateTimeService;

	public virtual void Copy(string sourceFileName, string destFileName) { }
	public virtual void Copy(string sourceFileName, string destFileName, bool overwrite) { }
	public virtual void Move(string sourceFileName, string destFileName) { }
	public virtual void Move(string sourceFileName, string destFileName, bool overwrite) { }
	public virtual void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName) { }
	public virtual void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName, bool ignoreMetadataErrors) { }
	public virtual void Delete(string path) { }
	public virtual bool Exists(string path) => false;
	public virtual void SetCreationTime(string path, DateTime creationTime) { }
	public virtual void SetCreationTimeUtc(string path, DateTime creationTimeUtc) { }
	public virtual DateTime GetCreationTime(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual DateTime GetCreationTimeUtc(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual void SetLastAccessTime(string path, DateTime lastAccessTime) { }
	public virtual void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) { }
	public virtual DateTime GetLastAccessTime(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual DateTime GetLastAccessTimeUtc(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual void SetLastWriteTime(string path, DateTime lastWriteTime) { }
	public virtual void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) { }
	public virtual DateTime GetLastWriteTime(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual DateTime GetLastWriteTimeUtc(string path) => _dateTimeService.UtcNow.AddDays(_addDays);
	public virtual FileAttributes GetAttributes(string path) => FileAttributes.Normal;
	public virtual void SetAttributes(string path, FileAttributes fileAttributes) { }

	public virtual FileStream Create(string path) => null!;
	public virtual FileStream Create(string path, int bufferSize) => null!;
	public virtual FileStream Create(string path, int bufferSize, FileOptions options) => null!;
	public virtual FileStream Open(string path, FileStreamOptions options) => null!;
	public virtual FileStream Open(string path, FileMode mode) => null!;
	public virtual FileStream Open(string path, FileMode mode, FileAccess access) => null!;
	public virtual FileStream Open(string path, FileMode mode, FileAccess access, FileShare share) => null!;
	public virtual FileStream OpenRead(string path) => null!;
	public virtual FileStream OpenWrite(string path) => null!;
	public virtual StreamReader OpenText(string path) => new(new MemoryStream(), Encoding.UTF8, false, 1024, false);
	public virtual StreamWriter CreateText(string path) => new(new MemoryStream(), Encoding.UTF8, 1024, false);
	public virtual StreamWriter AppendText(string path) => new(new MemoryStream(), Encoding.UTF8, 1024, false);

	public virtual string ReadAllText(string path) => string.Empty;
	public virtual string ReadAllText(string path, Encoding encoding) => string.Empty;
	public virtual Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
	public virtual Task<string> ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => Task.FromResult(string.Empty);
	public virtual void WriteAllText(string path, string contents) { }
	public virtual void WriteAllText(string path, string contents, Encoding encoding) { }
	public virtual Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual Task WriteAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default) => Task.CompletedTask;

	public virtual byte[] ReadAllBytes(string path) => [];
	public virtual Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult<byte[]>([]);
	public virtual void WriteAllBytes(string path, byte[] bytes) { }
	public virtual Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual string[] ReadAllLines(string path) => [];
	public virtual string[] ReadAllLines(string path, Encoding encoding) => [];
	public virtual Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default) => Task.FromResult<string[]>([]);
	public virtual Task<string[]> ReadAllLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => Task.FromResult<string[]>([]);
	public virtual IEnumerable<string> ReadLines(string path) => [];
	public virtual IEnumerable<string> ReadLines(string path, Encoding encoding) => [];
	public virtual IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default) => EmptyLinesAsync();
	public virtual IAsyncEnumerable<string> ReadLinesAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) => EmptyLinesAsync();
	public virtual void WriteAllLines(string path, IEnumerable<string> contents) { }
	public virtual void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding) { }
	public virtual Task WriteAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual Task WriteAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual void AppendAllText(string path, string contents) { }
	public virtual void AppendAllText(string path, string contents, Encoding encoding) { }
	public virtual Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual Task AppendAllTextAsync(string path, string contents, Encoding encoding, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual void AppendAllLines(string path, IEnumerable<string> contents) { }
	public virtual void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding) { }
	public virtual Task AppendAllLinesAsync(string path, IEnumerable<string> contents, CancellationToken cancellationToken = default) => Task.CompletedTask;
	public virtual Task AppendAllLinesAsync(string path, IEnumerable<string> contents, Encoding encoding, CancellationToken cancellationToken = default) => Task.CompletedTask;

	private static async IAsyncEnumerable<string> EmptyLinesAsync()
	{
		yield break;
	}
}
