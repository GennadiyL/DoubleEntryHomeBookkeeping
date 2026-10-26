namespace Master.Contracts.Services.Synchronizations;

/// <summary>
/// Returns the business result of acknowledging an installed Local snapshot.
/// IsSuccess confirms that Master accepted the acknowledgement.
/// A false value does not confirm completion of the Local synchronization.
/// The result carries no credentials or database snapshot content.
/// It follows the same result-record pattern as other Master operations.
/// Idempotent retries retain the acknowledgement contract's semantics.
/// Client uses successful acknowledgement to complete its pending workflow.
/// Transport failures remain separate from this business result.
/// </summary>
public record AcknowledgeResponse
{
	public bool IsSuccess { get; set; }
}
