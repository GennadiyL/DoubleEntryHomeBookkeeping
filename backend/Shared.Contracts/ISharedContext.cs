namespace Shared.Contracts;

public interface ISharedContext
{
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
