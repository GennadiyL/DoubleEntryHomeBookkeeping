using Business.Contracts.Params;

namespace Dehb.WebApi.Params;

public class UpdateAccountParam : AccountParam
{
	public Guid EntityId { get; set; }
}
