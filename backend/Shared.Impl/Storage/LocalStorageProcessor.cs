using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Shared.Impl.Storage;

/// <summary>
/// Stores objects as files below the root supplied by its factory.
/// Delegates file access to IFileService and owns only the streams it opens.
/// The requested directory and any object parent directories must already exist.
/// Uses file attributes to distinguish missing objects from access failures.
/// Receives IFileService directly to avoid a dependency cycle through ISharedContext.
/// Retains its resolved root while each operation owns its own file streams.
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

	public async Task Read(string objectName, Stream destination, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(destination);
		if (!destination.CanWrite)
		{
			throw new ArgumentException("The destination must be writable.", nameof(destination));
		}

		string path = PreparePath(objectName, cancellationToken);
		await using FileStream source = Open(path, FileMode.Open, FileAccess.Read, FileShare.Read);
		await source.CopyToAsync(destination, cancellationToken);
	}

	public async Task Write(string objectName, Stream source, bool overwrite, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(source);
		if (!source.CanRead)
		{
			throw new ArgumentException("The source must be readable.", nameof(source));
		}

		string path = PreparePath(objectName, cancellationToken);
		FileMode mode = overwrite ? FileMode.Create : FileMode.CreateNew;
		await using FileStream destination = Open(path, mode, FileAccess.Write, FileShare.None);
		await source.CopyToAsync(destination, cancellationToken);
	}

	public Task Delete(string objectName, CancellationToken cancellationToken = default)
	{
		string path = PreparePath(objectName, cancellationToken);
		try
		{
			_fileService.Delete(path);
		}
		catch (IOException exception) when (IsMissing(exception))
		{
			EnsureRootAvailable(_root);
		}

		return Task.CompletedTask;
	}

	public Task<bool> Exists(string objectName, CancellationToken cancellationToken = default)
	{
		string path = PreparePath(objectName, cancellationToken);
		try
		{
			EnsureFile(_fileService.GetAttributes(path));
			return Task.FromResult(true);
		}
		catch (IOException exception) when (IsMissing(exception))
		{
			EnsureRootAvailable(_root);
			return Task.FromResult(false);
		}
	}

	private string PreparePath(string objectName, CancellationToken cancellationToken)
	{
		cancellationToken.ThrowIfCancellationRequested();
		ArgumentException.ThrowIfNullOrWhiteSpace(objectName);
		string path = ResolveObjectPath(_root, objectName);
		EnsureRootAvailable(_root);
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

	private static bool IsMissing(IOException exception)
	{
		return exception is FileNotFoundException or DirectoryNotFoundException;
	}
}
