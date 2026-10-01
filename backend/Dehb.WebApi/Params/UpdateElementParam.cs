using Business.Contracts.Services.Trees;

namespace Dehb.WebApi.Params;

public record UpdateElementParam : ElementParam
{
	public Guid EntityId { get; set; }
}
