using Business.Contracts.Params;

namespace Dehb.WebApi.Params;

public class UpdateGroupParam : GroupParam
{
	public Guid EntityId { get; set; }
}
