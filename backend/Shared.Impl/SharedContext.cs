using Shared.Contracts;

namespace Shared.Impl;

/// <summary>
/// Defines the shared service context.
/// Provides a POCO container for project-independent services consumed by business methods.
/// The dependency injection container creates a scoped context from registered shared services.
/// Business code receives ISharedContext instead of coupling to concrete implementations.
/// It contains no business state or service behavior of its own.
/// </summary>
internal class SharedContext : ISharedContext
{
	public SharedContext(
		IDateTimeService dateTimeService,
		ITlsService tlsService,
		ILogService logService,
		IMessageService busService,
		IJsonService jsonService,
		IXmlService xmlService,
		IZipService zipService,
		IFileService fileService,
		IDirectoryService directoryService)
	{
		DateTimeService = dateTimeService;
		TlsService = tlsService;
		LogService = logService;
		MessageService = busService;
		JsonService = jsonService;
		XmlService = xmlService;
		ZipService = zipService;
		FileService = fileService;
		DirectoryService = directoryService;
	}

	public IDateTimeService DateTimeService { get; }

	public ITlsService TlsService { get; }

	public ILogService LogService { get; }

	public IMessageService MessageService { get; }

	public IJsonService JsonService { get; }

	public IXmlService XmlService { get; }

	public IZipService ZipService { get; }

	public IFileService FileService { get; }

	public IDirectoryService DirectoryService { get; }
}
