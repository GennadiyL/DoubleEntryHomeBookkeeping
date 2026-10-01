using Business.Contracts.Services.Trees;

namespace Dehb.WebApi.Params;

public record UpdateGroupParam : GroupParam
{
	public Guid EntityId { get; set; }
}
