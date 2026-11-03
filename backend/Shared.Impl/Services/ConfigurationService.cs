using Shared.Contracts;
using Shared.Contracts.Config;

namespace Shared.Impl.Services;

/// <summary>
/// Owns one temporary directory for a dependency scope.
/// The directory is allocated lazily on the first request.
/// All consumers in that scope receive the same cached path.
/// Directory operations are delegated to the injected directory service.
/// A missing directory is recreated when requested again.
/// Concurrent requests share the same allocation and disposal boundary.
/// Scope disposal removes only this service's generated directory and its contents.
/// Both synchronous and asynchronous scope disposal release the directory.
/// Persistent storage paths and business dataset names are outside this service.
/// </summary>
internal sealed class ConfigurationService : IConfigurationService, IDisposable, IAsyncDisposable
{
	private readonly IDirectoryService _directoryService;
	private readonly object _gate = new();
	private string? _tempDirectory;
	private bool _disposed;

	public ConfigurationService(IDirectoryService directoryService) => _directoryService = directoryService;

	public string GetTempDirectory()
	{
		lock (_gate)
		{
			ObjectDisposedException.ThrowIf(_disposed, this);
			_tempDirectory ??= Path.Combine(Path.GetTempPath(), $"dehb-{Guid.NewGuid():N}");
			_directoryService.CreateDirectory(_tempDirectory);
			return _tempDirectory;
		}
	}

	public void Dispose()
	{
		lock (_gate)
		{
			if (_disposed)
			{
				return;
			}
			if (_tempDirectory is not null && _directoryService.Exists(_tempDirectory))
			{
				_directoryService.Delete(_tempDirectory, true);
			}
			_disposed = true;
		}
	}

	public ValueTask DisposeAsync()
	{
		Dispose();
		return ValueTask.CompletedTask;
	}
}
