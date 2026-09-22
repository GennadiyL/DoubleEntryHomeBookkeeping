using System.Text;
using Shared.Contracts;

namespace Shared.Impl.Services;

/// <summary>
/// Defines the file service.
/// Wraps platform file operations behind the project-independent service contract.
/// Consumers resolve IFileService instead of calling static File methods.
/// The implementation supports substitution in tests and reuse across solutions.
/// It does not interpret business file formats.
/// </summary>
internal class FileService : IFileService
{
	public void Copy(string sourceFileName, string destFileName) => File.Copy(sourceFileName, destFileName);

	public void Copy(string sourceFileName, string destFileName, bool overwrite) =>
		File.Copy(sourceFileName, destFileName, overwrite);

	public void Move(string sourceFileName, string destFileName) => File.Move(sourceFileName, destFileName);

	public void Move(string sourceFileName, string destFileName, bool overwrite) =>
		File.Move(sourceFileName, destFileName, overwrite);

	public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName) =>
		File.Replace(sourceFileName, destinationFileName, destinationBackupFileName);

	public void Replace(
		string sourceFileName,
		string destinationFileName,
		string destinationBackupFileName,
		bool ignoreMetadataErrors) =>
		File.Replace(sourceFileName, destinationFileName, destinationBackupFileName, ignoreMetadataErrors);

	public void Delete(string path) => File.Delete(path);

	public bool Exists(string path) => File.Exists(path);

	public void SetCreationTime(string path, DateTime creationTime) => File.SetCreationTime(path, creationTime);

	public void SetCreationTimeUtc(string path, DateTime creationTimeUtc) =>
		File.SetCreationTimeUtc(path, creationTimeUtc);

	public DateTime GetCreationTime(string path) => File.GetCreationTime(path);

	public DateTime GetCreationTimeUtc(string path) => File.GetCreationTimeUtc(path);

	public void SetLastAccessTime(string path, DateTime lastAccessTime) => File.SetLastAccessTime(path, lastAccessTime);

	public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc) =>
		File.SetLastAccessTimeUtc(path, lastAccessTimeUtc);

	public DateTime GetLastAccessTime(string path) => File.GetLastAccessTime(path);

	public DateTime GetLastAccessTimeUtc(string path) => File.GetLastAccessTimeUtc(path);

	public void SetLastWriteTime(string path, DateTime lastWriteTime) => File.SetLastWriteTime(path, lastWriteTime);

	public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc) =>
		File.SetLastWriteTimeUtc(path, lastWriteTimeUtc);

	public DateTime GetLastWriteTime(string path) => File.GetLastWriteTime(path);

	public DateTime GetLastWriteTimeUtc(string path) => File.GetLastWriteTimeUtc(path);

	public FileAttributes GetAttributes(string path) => File.GetAttributes(path);

	public void SetAttributes(string path, FileAttributes fileAttributes) => File.SetAttributes(path, fileAttributes);

	public FileStream Create(string path) => File.Create(path);

	public FileStream Create(string path, int bufferSize) => File.Create(path, bufferSize);

	public FileStream Create(string path, int bufferSize, FileOptions options) =>
		File.Create(path, bufferSize, options);

	public FileStream Open(string path, FileStreamOptions options) => File.Open(path, options);

	public FileStream Open(string path, FileMode mode) => File.Open(path, mode);

	public FileStream Open(string path, FileMode mode, FileAccess access) => File.Open(path, mode, access);

	public FileStream Open(string path, FileMode mode, FileAccess access, FileShare share) =>
		File.Open(path, mode, access, share);

	public FileStream OpenRead(string path) => File.OpenRead(path);

	public FileStream OpenWrite(string path) => File.OpenWrite(path);

	public StreamReader OpenText(string path) => File.OpenText(path);

	public StreamWriter CreateText(string path) => File.CreateText(path);

	public StreamWriter AppendText(string path) => File.AppendText(path);

	public string ReadAllText(string path) => File.ReadAllText(path);

	public string ReadAllText(string path, Encoding encoding) => File.ReadAllText(path, encoding);

	public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default) =>
		File.ReadAllTextAsync(path, cancellationToken);

	public Task<string>
		ReadAllTextAsync(string path, Encoding encoding, CancellationToken cancellationToken = default) =>
		File.ReadAllTextAsync(path, encoding, cancellationToken);

	public void WriteAllText(string path, string contents) => File.WriteAllText(path, contents);

	public void WriteAllText(string path, string contents, Encoding encoding) =>
		File.WriteAllText(path, contents, encoding);

	public Task WriteAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) =>
		File.WriteAllTextAsync(path, contents, cancellationToken);

	public Task WriteAllTextAsync(
		string path,
		string contents,
		Encoding encoding,
		CancellationToken cancellationToken = default) =>
		File.WriteAllTextAsync(path, contents, encoding, cancellationToken);

	public byte[] ReadAllBytes(string path) => File.ReadAllBytes(path);

	public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default) =>
		File.ReadAllBytesAsync(path, cancellationToken);

	public void WriteAllBytes(string path, byte[] bytes) => File.WriteAllBytes(path, bytes);

	public Task WriteAllBytesAsync(string path, byte[] bytes, CancellationToken cancellationToken = default) =>
		File.WriteAllBytesAsync(path, bytes, cancellationToken);

	public string[] ReadAllLines(string path) => File.ReadAllLines(path);

	public string[] ReadAllLines(string path, Encoding encoding) => File.ReadAllLines(path, encoding);

	public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default) =>
		File.ReadAllLinesAsync(path, cancellationToken);

	public Task<string[]> ReadAllLinesAsync(
		string path,
		Encoding encoding,
		CancellationToken cancellationToken = default) =>
		File.ReadAllLinesAsync(path, encoding, cancellationToken);

	public IEnumerable<string> ReadLines(string path) => File.ReadLines(path);

	public IEnumerable<string> ReadLines(string path, Encoding encoding) => File.ReadLines(path, encoding);

	public IAsyncEnumerable<string> ReadLinesAsync(string path, CancellationToken cancellationToken = default) =>
		File.ReadLinesAsync(path, cancellationToken);

	public IAsyncEnumerable<string> ReadLinesAsync(
		string path,
		Encoding encoding,
		CancellationToken cancellationToken = default) =>
		File.ReadLinesAsync(path, encoding, cancellationToken);

	public void WriteAllLines(string path, IEnumerable<string> contents) => File.WriteAllLines(path, contents);

	public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding) =>
		File.WriteAllLines(path, contents, encoding);

	public Task WriteAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		CancellationToken cancellationToken = default) =>
		File.WriteAllLinesAsync(path, contents, cancellationToken);

	public Task WriteAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		Encoding encoding,
		CancellationToken cancellationToken = default) =>
		File.WriteAllLinesAsync(path, contents, encoding, cancellationToken);

	public void AppendAllText(string path, string contents) => File.AppendAllText(path, contents);

	public void AppendAllText(string path, string contents, Encoding encoding) =>
		File.AppendAllText(path, contents, encoding);

	public Task AppendAllTextAsync(string path, string contents, CancellationToken cancellationToken = default) =>
		File.AppendAllTextAsync(path, contents, cancellationToken);

	public Task AppendAllTextAsync(
		string path,
		string contents,
		Encoding encoding,
		CancellationToken cancellationToken = default) =>
		File.AppendAllTextAsync(path, contents, encoding, cancellationToken);

	public void AppendAllLines(string path, IEnumerable<string> contents) => File.AppendAllLines(path, contents);

	public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding) =>
		File.AppendAllLines(path, contents, encoding);

	public Task AppendAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		CancellationToken cancellationToken = default) =>
		File.AppendAllLinesAsync(path, contents, cancellationToken);

	public Task AppendAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		Encoding encoding,
		CancellationToken cancellationToken = default) =>
		File.AppendAllLinesAsync(path, contents, encoding, cancellationToken);
}
