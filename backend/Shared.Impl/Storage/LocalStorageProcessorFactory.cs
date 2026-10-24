using Microsoft.Extensions.Configuration;
using Shared.Contracts;
using Shared.Contracts.Storage;

namespace Shared.Impl.Storage;

/// <summary>
/// Creates Local processors using the host's masterFileStorage configuration.
/// Reads the root for each creation and resolves relative paths against the application directory.
/// Each processor keeps its own resolved root for its lifetime.
/// The factory retains dependencies only and does not cache processors or perform file access.
/// Missing configuration is reported when a processor is requested.
/// </summary>
internal sealed class LocalStorageProcessorFactory : IStorageProcessorFactory
{
	private readonly IFileService _fileService;
	private readonly IConfiguration _configuration;

	public LocalStorageProcessorFactory(IFileService fileService, IConfiguration configuration)
	{
		_fileService = fileService;
		_configuration = configuration;
	}

	public IStorageProcessor CreateProcessor()
	{
		string? root = _configuration["masterFileStorage:Root"];
		if (string.IsNullOrWhiteSpace(root))
		{
			throw new InvalidOperationException("Configure masterFileStorage:Root before creating a Local storage processor.");
		}

		string fullRoot = Path.GetFullPath(root, AppContext.BaseDirectory);
		return new LocalStorageProcessor(_fileService, fullRoot);
	}
}
