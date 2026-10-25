using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Shared.Impl.Storage;

/// <summary>
/// Stores objects as files below the root supplied by its factory.
/// Delegates file access to IFileService and owns only the streams it opens.
/// The requested directory and any object parent directories must already exist.
/// Optional locks are borrowed and their ownership-loss tokens are observed.
/// Local locking is cooperative; the caller selects and retains the appropriate lock.
/// Uses file attributes to distinguish missing objects from access failures.
/// Receives IFileService directly to avoid a dependency cycle through ISharedContext.
/// </summary>
internal sealed class LocalStorageProcessor : IStorageProcessor
{
	private readonly IFileService _fileService;
	private readonly string _root;

	public LocalStorageProcessor(IFileService fileService, string root)
	{
		_fileService = fileService;
		ArgumentException.ThrowIfNullOrWhiteSpace(root);
		_root = Path.GetFullPath(root);
	}

	public async Task Read(string objectName, Stream destination, IStorageLock? storageLock = null,
		CancellationToken cancellationToken = default)
	{
		ValidateStream(destination, true);
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		string path = PreparePath(objectName, cancellation.Token);
		await using FileStream source = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		await source.CopyToAsync(destination, cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public async Task Write(string objectName, Stream source, bool overwrite, IStorageLock? storageLock = null,
		CancellationToken cancellationToken = default)
	{
		ValidateStream(source, false);
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		string path = PreparePath(objectName, cancellation.Token);
		FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;
		await using FileStream destination = Open(path, mode, FileAccess.Write, FileShare.None);
		await source.CopyToAsync(destination, cancellation.Token);
		await destination.FlushAsync(cancellation.Token);
		cancellation.Token.ThrowIfCancellationRequested();
	}

	public Task Delete(string objectName, IStorageLock? storageLock = null, CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		string path = PreparePath(objectName, cancellation.Token);
		DeleteFile(path);
		cancellation.Token.ThrowIfCancellationRequested();
		return Task.CompletedTask;
	}

	public Task<bool> Exists(string objectName, IStorageLock? storageLock = null, CancellationToken cancellationToken = default)
	{
		using CancellationTokenSource cancellation = LinkCancellation(storageLock, cancellationToken);
		string path = PreparePath(objectName, cancellation.Token);
		bool exists = FileExists(path);
		cancellation.Token.ThrowIfCancellationRequested();
		return Task.FromResult(exists);
	}

	private void DeleteFile(string path)
	{
		try
		{
			_fileService.Delete(path);
		}
		catch (IOException exception) when (IsMissing(exception))
		{
			EnsureRootAvailable(_root);
		}
	}

	private bool FileExists(string path)
	{
		try
		{
			EnsureFile(_fileService.GetAttributes(path));
			return true;
		}
		catch (IOException exception) when (IsMissing(exception))
		{
			EnsureRootAvailable(_root);
			return false;
		}
	}

	private string PreparePath(string objectName, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
		string path = ResolveObjectPath(_root, objectName);
		EnsureRootAvailable(_root);
		cancellationToken.ThrowIfCancellationRequested();
		return path;
	}

	private static string ResolveObjectPath(string root, string objectName)
	{
		string path = Path.GetFullPath(Path.Combine(root, objectName));
		string relative = Path.GetRelativePath(root, path);
		if (Path.IsPathRooted(objectName) || relative == "." || relative == ".." ||
			relative.StartsWith($"..{Path.DirectorySeparatorChar}", StringComparison.Ordinal) || Path.IsPathRooted(relative))
		{
			throw new ArgumentException("Object names must identify files below the storage root.", nameof(objectName));
		}

		return path;
	}

	private void EnsureRootAvailable(string root)
	{
		if (!_fileService.GetAttributes(root).HasFlag(FileAttributes.Directory))
		{
			throw new IOException("The storage root must be a directory.");
		}
	}

	private static void EnsureFile(FileAttributes attributes)
	{
		if (attributes.HasFlag(FileAttributes.Directory))
		{
			throw new IOException("A directory cannot be used as a storage object.");
		}
	}

	private FileStream Open(string path, FileMode mode, FileAccess access, FileShare share)
	{
		return _fileService.Open(path, new FileStreamOptions
		{
			Mode = mode,
			Access = access,
			Share = share,
			Options = FileOptions.Asynchronous | FileOptions.SequentialScan
		});
	}

	private static CancellationTokenSource LinkCancellation(IStorageLock? storageLock, CancellationToken cancellationToken)
	{
		return CancellationTokenSource.CreateLinkedTokenSource(
			cancellationToken, storageLock?.CancellationToken ?? CancellationToken.None);
	}

	private static void ValidateStream(Stream stream, bool destination)
	{
		ArgumentNullException.ThrowIfNull(stream);
		if (destination ? !stream.CanWrite : !stream.CanRead)
		{
			throw new ArgumentException(destination ? "The destination must be writable." : "The source must be readable.", nameof(stream));
		}
	}

	private static bool IsMissing(IOException exception) =>
		exception is FileNotFoundException or DirectoryNotFoundException;
}
