using System.Text;

namespace Shared.Contracts;

public interface IFileService
{
	/// <summary>
	/// Performs the Copy operation through the injectable file-system service contract.
	/// </summary>
	public void Copy(string sourceFileName, string destFileName);
	/// <summary>
	/// Performs the Copy operation through the injectable file-system service contract.
	/// </summary>
	public void Copy(string sourceFileName, string destFileName, bool overwrite);
	/// <summary>
	/// Performs the Move operation through the injectable file-system service contract.
	/// </summary>
	public void Move(string sourceFileName, string destFileName);
	/// <summary>
	/// Performs the Move operation through the injectable file-system service contract.
	/// </summary>
	public void Move(string sourceFileName, string destFileName, bool overwrite);
	/// <summary>
	/// Performs the Replace operation through the injectable file-system service contract.
	/// </summary>
	public void Replace(string sourceFileName, string destinationFileName, string destinationBackupFileName);
	/// <summary>
	/// Performs the Replace operation through the injectable file-system service contract.
	/// </summary>
	public void Replace(
		string sourceFileName,
		string destinationFileName,
		string destinationBackupFileName,
		bool ignoreMetadataErrors);
	/// <summary>
	/// Performs the Delete operation through the injectable file-system service contract.
	/// </summary>
	public void Delete(string path);
	/// <summary>
	/// Performs the Exists operation through the injectable file-system service contract.
	/// </summary>
	public bool Exists(string path);


	/// <summary>
	/// Performs the SetCreationTime operation through the injectable file-system service contract.
	/// </summary>
	public void SetCreationTime(string path, DateTime creationTime);
	/// <summary>
	/// Performs the SetCreationTimeUtc operation through the injectable file-system service contract.
	/// </summary>
	public void SetCreationTimeUtc(string path, DateTime creationTimeUtc);
	/// <summary>
	/// Performs the GetCreationTime operation through the injectable file-system service contract.
	/// </summary>
	public DateTime GetCreationTime(string path);
	/// <summary>
	/// Performs the GetCreationTimeUtc operation through the injectable file-system service contract.
	/// </summary>
	public DateTime GetCreationTimeUtc(string path);
	/// <summary>
	/// Performs the SetLastAccessTime operation through the injectable file-system service contract.
	/// </summary>
	public void SetLastAccessTime(string path, DateTime lastAccessTime);
	/// <summary>
	/// Performs the SetLastAccessTimeUtc operation through the injectable file-system service contract.
	/// </summary>
	public void SetLastAccessTimeUtc(string path, DateTime lastAccessTimeUtc);
	/// <summary>
	/// Performs the GetLastAccessTime operation through the injectable file-system service contract.
	/// </summary>
	public DateTime GetLastAccessTime(string path);
	/// <summary>
	/// Performs the GetLastAccessTimeUtc operation through the injectable file-system service contract.
	/// </summary>
	public DateTime GetLastAccessTimeUtc(string path);
	/// <summary>
	/// Performs the SetLastWriteTime operation through the injectable file-system service contract.
	/// </summary>
	public void SetLastWriteTime(string path, DateTime lastWriteTime);
	/// <summary>
	/// Performs the SetLastWriteTimeUtc operation through the injectable file-system service contract.
	/// </summary>
	public void SetLastWriteTimeUtc(string path, DateTime lastWriteTimeUtc);
	/// <summary>
	/// Performs the GetLastWriteTime operation through the injectable file-system service contract.
	/// </summary>
	public DateTime GetLastWriteTime(string path);
	/// <summary>
	/// Performs the GetLastWriteTimeUtc operation through the injectable file-system service contract.
	/// </summary>
	public DateTime GetLastWriteTimeUtc(string path);
	/// <summary>
	/// Performs the GetAttributes operation through the injectable file-system service contract.
	/// </summary>
	public FileAttributes GetAttributes(string path);
	/// <summary>
	/// Performs the SetAttributes operation through the injectable file-system service contract.
	/// </summary>
	public void SetAttributes(string path, FileAttributes fileAttributes);


	/// <summary>
	/// Performs the Create operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Create(string path);
	/// <summary>
	/// Performs the Create operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Create(string path, int bufferSize);
	/// <summary>
	/// Performs the Create operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Create(string path, int bufferSize, FileOptions options);
	/// <summary>
	/// Performs the Open operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Open(string path, FileStreamOptions options);
	/// <summary>
	/// Performs the Open operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Open(string path, FileMode mode);
	/// <summary>
	/// Performs the Open operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Open(string path, FileMode mode, FileAccess access);
	/// <summary>
	/// Performs the Open operation through the injectable file-system service contract.
	/// </summary>
	public FileStream Open(string path, FileMode mode, FileAccess access, FileShare share);
	/// <summary>
	/// Performs the OpenRead operation through the injectable file-system service contract.
	/// </summary>
	public FileStream OpenRead(string path);
	/// <summary>
	/// Performs the OpenWrite operation through the injectable file-system service contract.
	/// </summary>
	public FileStream OpenWrite(string path);
	/// <summary>
	/// Performs the OpenText operation through the injectable file-system service contract.
	/// </summary>
	public StreamReader OpenText(string path);
	/// <summary>
	/// Performs the CreateText operation through the injectable file-system service contract.
	/// </summary>
	public StreamWriter CreateText(string path);
	/// <summary>
	/// Performs the AppendText operation through the injectable file-system service contract.
	/// </summary>
	public StreamWriter AppendText(string path);


	/// <summary>
	/// Performs the ReadAllText operation through the injectable file-system service contract.
	/// </summary>
	public string ReadAllText(string path);
	/// <summary>
	/// Performs the ReadAllText operation through the injectable file-system service contract.
	/// </summary>
	public string ReadAllText(string path, Encoding encoding);
	/// <summary>
	/// Performs the ReadAllTextAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the ReadAllTextAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task<string> ReadAllTextAsync(
		string path,
		Encoding encoding,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Performs the WriteAllText operation through the injectable file-system service contract.
	/// </summary>
	public void WriteAllText(string path, string contents);
	/// <summary>
	/// Performs the WriteAllText operation through the injectable file-system service contract.
	/// </summary>
	public void WriteAllText(string path, string contents, Encoding encoding);
	/// <summary>
	/// Performs the WriteAllTextAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task WriteAllTextAsync(
		string path,
		string contents,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the WriteAllTextAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task WriteAllTextAsync(
		string path,
		string contents,
		Encoding encoding,
		CancellationToken cancellationToken = default);


	/// <summary>
	/// Performs the ReadAllBytes operation through the injectable file-system service contract.
	/// </summary>
	public byte[] ReadAllBytes(string path);
	/// <summary>
	/// Performs the ReadAllBytesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task<byte[]> ReadAllBytesAsync(string path, CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the WriteAllBytes operation through the injectable file-system service contract.
	/// </summary>
	public void WriteAllBytes(string path, byte[] bytes);
	/// <summary>
	/// Performs the WriteAllBytesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task WriteAllBytesAsync(
		string path,
		byte[] bytes,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the ReadAllLines operation through the injectable file-system service contract.
	/// </summary>
	public string[] ReadAllLines(string path);
	/// <summary>
	/// Performs the ReadAllLines operation through the injectable file-system service contract.
	/// </summary>
	public string[] ReadAllLines(string path, Encoding encoding);
	/// <summary>
	/// Performs the ReadAllLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task<string[]> ReadAllLinesAsync(string path, CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the ReadAllLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task<string[]> ReadAllLinesAsync(
		string path,
		Encoding encoding,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the ReadLines operation through the injectable file-system service contract.
	/// </summary>
	public IEnumerable<string> ReadLines(string path);
	/// <summary>
	/// Performs the ReadLines operation through the injectable file-system service contract.
	/// </summary>
	public IEnumerable<string> ReadLines(string path, Encoding encoding);
	/// <summary>
	/// Performs the ReadLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public IAsyncEnumerable<string> ReadLinesAsync(
		string path,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the ReadLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public IAsyncEnumerable<string> ReadLinesAsync(
		string path,
		Encoding encoding,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the WriteAllLines operation through the injectable file-system service contract.
	/// </summary>
	public void WriteAllLines(string path, IEnumerable<string> contents);
	/// <summary>
	/// Performs the WriteAllLines operation through the injectable file-system service contract.
	/// </summary>
	public void WriteAllLines(string path, IEnumerable<string> contents, Encoding encoding);
	/// <summary>
	/// Performs the WriteAllLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task WriteAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the WriteAllLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task WriteAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		Encoding encoding,
		CancellationToken cancellationToken = default);

	/// <summary>
	/// Performs the AppendAllText operation through the injectable file-system service contract.
	/// </summary>
	public void AppendAllText(string path, string contents);
	/// <summary>
	/// Performs the AppendAllText operation through the injectable file-system service contract.
	/// </summary>
	public void AppendAllText(string path, string contents, Encoding encoding);
	/// <summary>
	/// Performs the AppendAllTextAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task AppendAllTextAsync(
		string path,
		string contents,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the AppendAllTextAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task AppendAllTextAsync(
		string path,
		string contents,
		Encoding encoding,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the AppendAllLines operation through the injectable file-system service contract.
	/// </summary>
	public void AppendAllLines(string path, IEnumerable<string> contents);
	/// <summary>
	/// Performs the AppendAllLines operation through the injectable file-system service contract.
	/// </summary>
	public void AppendAllLines(string path, IEnumerable<string> contents, Encoding encoding);
	/// <summary>
	/// Performs the AppendAllLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task AppendAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		CancellationToken cancellationToken = default);
	/// <summary>
	/// Performs the AppendAllLinesAsync operation through the injectable file-system service contract.
	/// </summary>
	public Task AppendAllLinesAsync(
		string path,
		IEnumerable<string> contents,
		Encoding encoding,
		CancellationToken cancellationToken = default);
}
