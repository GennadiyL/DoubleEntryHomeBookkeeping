namespace Business.Contracts.Base.Services;

public interface IUpdateEntityService<in T>
	where T : class
{
	/// <summary>
	/// Creates an entity from validated input and returns its identity to the editor.
	/// Unknown or deleted references reject the complete save; data and sync flags commit together.
	/// Local failures are reported without automatic retry.
	/// </summary>
	public Task<Guid> Add(T param);

	/// <summary>
	/// Saves editor changes while preserving the entity identity.
	/// Unknown or deleted targets throw not-found; invalid references reject the complete save.
	/// Transaction and Template updates replace all entries with new rows in submitted order.
	/// All changes and sync flags commit together or roll back; failures are not automatically retried.
	/// </summary>
	public Task Update(Guid entityId, T param);

	/// <summary>
	/// Deletes an entity through its normal lifecycle for the maintenance screen.
	/// Unknown or already-deleted targets throw not-found without changes.
	/// Entity-specific restrictions apply; aggregate changes and sync tracking commit together.
	/// Failures roll back the action and are reported without automatic retry.
	/// </summary>
	public Task Delete(Guid entityId);
}
