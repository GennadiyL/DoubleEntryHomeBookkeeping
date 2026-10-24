namespace Shared.Contracts.Storage;

/// <summary>
/// Provides asynchronous object storage independent of the application domain.
/// The factory supplies connection settings; operations use object names relative to that fixed location.
/// Streams are read or written from their current positions and remain caller-owned.
/// Operations propagate access and storage failures, including an unavailable root or container.
/// </summary>
public interface IStorageProcessor
{
	/// <summary>
	/// Copies an existing object into a writable destination stream without closing it.
	/// A missing object throws; failed reads may leave partial data in the destination.
	/// </summary>
	public Task Read(string objectName, Stream destination, CancellationToken cancellationToken = default);

	/// <summary>
	/// Copies a readable source stream into storage without closing it.
	/// Rejects an existing object unless overwrite is true; parent directories must already exist locally.
	/// Failed or cancelled writes may leave partial data; this contract does not guarantee atomic writes.
	/// </summary>
	public Task Write(string objectName, Stream source, bool overwrite, CancellationToken cancellationToken = default);

	/// <summary>
	/// Deletes an object. An already missing object is a successful no-op.
	/// </summary>
	public Task Delete(string objectName, CancellationToken cancellationToken = default);

	/// <summary>
	/// Returns false only when the object is missing; access and storage failures propagate.
	/// </summary>
	public Task<bool> Exists(string objectName, CancellationToken cancellationToken = default);
}
