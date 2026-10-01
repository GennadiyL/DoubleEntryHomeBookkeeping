namespace Business.Contracts.Base.Services;

public interface IReadEntityService<T>
{
	/// <summary>
	/// Loads the entity values and reference labels needed to open its edit dialog.
	/// Returns a detached information record without saving changes.
	/// Unknown or deleted identities throw a not-found exception; no null or empty record is returned.
	/// </summary>
	public Task<T> GetById(Guid id);
}
