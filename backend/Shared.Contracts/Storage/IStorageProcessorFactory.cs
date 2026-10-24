namespace Shared.Contracts.Storage;

/// <summary>
/// Creates processors configured for the storage selected by the host.
/// Each processor retains the connection settings observed at creation.
/// The factory does not own processor operation state or perform storage I/O.
/// </summary>
public interface IStorageProcessorFactory
{
	/// <summary>
	/// Creates an independent processor from current configuration for subsequent object operations.
	/// Missing or invalid required settings throw before a processor is returned.
	/// </summary>
	public IStorageProcessor CreateProcessor();
}
