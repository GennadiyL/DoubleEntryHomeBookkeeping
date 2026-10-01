namespace Business.Contracts.Base.Services;

/// <summary>
/// Defines the shared local create, update and soft-delete lifecycle.
/// Concrete services apply their entity-specific validation and reference rules.
/// Creation starts with null revisions and None flags; edits add applicable flags.
/// Transaction and template edits replace their complete owned entry collections.
/// Each successful state-changing action commits once with tracking or rolls back.
/// Validation failures and missing targets make no changes; writes are not automatically retried.
/// </summary>
public interface IUpdateEntityService<in T>
	where T : class
{
	/// <summary>
	/// Creates a validated entity and returns its identity to the editor.
	/// Initializes EditRevision and DeleteRevision to null and ModificationType to None.
	/// Creation is detected by null EditRevision; initial content and order need no edit flags.
	/// Unknown or deleted references reject the complete save.
	/// All new rows and tracking metadata commit together; local failures are not automatically retried.
	/// </summary>
	public Task<Guid> Add(T param, CancellationToken cancellationToken = default);

	/// <summary>
	/// Saves editor changes while preserving the entity identity and received edit revision.
	/// Unknown or deleted targets throw not-found; invalid references reject the complete save.
	/// Content changes add Content while preserving existing flags.
	/// Transaction and Template updates replace all entries with new rows in submitted zero-based order.
	/// All changes and tracking commit together or roll back; failures are not automatically retried.
	/// </summary>
	public Task Update(Guid entityId, T param, CancellationToken cancellationToken = default);

	/// <summary>
	/// Soft-deletes an entity for the maintenance screen, including a never-submitted creation.
	/// Unknown or already-deleted targets throw not-found; entity-specific restrictions still apply.
	/// Sets DeleteRevision to zero, preserves EditRevision and adds Content without clearing existing flags.
	/// Ordered catalogs normalize survivors and mark each changed position with Order.
	/// Hard deletion of a never-submitted deletion is deferred to atomic delta preparation.
	/// The complete action commits once or rolls back; failures are not automatically retried.
	/// </summary>
	public Task Delete(Guid entityId, CancellationToken cancellationToken = default);
}
