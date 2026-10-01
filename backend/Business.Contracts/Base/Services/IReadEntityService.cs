namespace Business.Contracts.Base.Services;

/// <summary>
/// Defines full entity reads for opening an editor.
/// Results are detached information records rather than persistent entities.
/// Unknown or deleted identities fail with a not-found exception.
/// Reading does not persist changes or alter synchronization metadata.
/// </summary>
public interface IReadEntityService<T>
{
	/// <summary>
	/// Loads the entity values and reference labels needed to open its edit dialog.
	/// Returns a detached information record without saving changes.
	/// Unknown or deleted identities throw a not-found exception; no null or empty record is returned.
	/// </summary>
	public Task<T> GetById(Guid id, CancellationToken cancellationToken = default);
}
