using Business.Contracts.Services.Accounts;

namespace Dehb.WebApi.Params;

public record UpdateAccountParam : AccountParam
{
	public Guid EntityId { get; set; }
}
