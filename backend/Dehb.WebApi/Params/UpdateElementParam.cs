using Business.Contracts.Params;

namespace Dehb.WebApi.Params;

public class UpdateElementParam : ElementParam
{
	public Guid EntityId { get; set; }
}
