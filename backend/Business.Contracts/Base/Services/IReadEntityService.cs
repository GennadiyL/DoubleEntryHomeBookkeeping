namespace Business.Contracts.Base.Services;

public interface IReadEntityService<T>
{
	/// <summary>
	/// Loads the entity values and reference labels needed to open its edit dialog.
	/// Returns a detached information record without saving changes.
	/// </summary>
	public Task<T> GetById(Guid id);
}
