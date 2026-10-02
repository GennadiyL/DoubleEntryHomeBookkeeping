using Business.Contracts.Services.Templates;

namespace Dehb.WebApi.Params;

/// <summary>
/// Binds the identity and complete editor values for a template update.
/// Inherits the template name, group, description and favorite selection.
/// Entries form the full replacement set in their submitted order.
/// EntityId identifies the existing template whose identity is preserved.
/// The endpoint separates that identity from the service input.
/// Validation and persistence belong to the template service.
/// This input contains no synchronization metadata or entry identities.
/// Moving a template uses the separate catalog move operation.
/// </summary>
public record UpdateTemplateParam : TemplateParam
{
	public Guid EntityId { get; set; }
}
