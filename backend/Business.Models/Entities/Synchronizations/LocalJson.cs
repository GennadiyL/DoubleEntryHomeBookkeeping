using Business.Core.Entities;
using Business.Models.Constants;

namespace Business.Models.Entities.Synchronizations;

/// <summary>
/// Stores the single pending synchronization JSON document for a Local database.
/// The inherited Guid identity defaults to EntitiesIds.LocalJsonId.
/// Json contains the serialized container retained for submission and retries.
/// Original batch identities remain inside that document.
/// The workflow coordinates persistence with acceptance of captured changes.
/// This Local-only metadata is not captured or merged as business content.
/// Database replacement must preserve pending content until completion.
/// Serialization, validation and recovery behavior belong to the workflow.
/// </summary>
public class LocalJson : BaseEntity
{
	public LocalJson() => Id = EntitiesIds.LocalJsonId;

	public string Json { get; set; } = string.Empty;
}
