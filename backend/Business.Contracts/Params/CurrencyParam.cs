using Business.Contracts.Params.Interfaces;

namespace Business.Contracts.Params;

public class CurrencyParam
{
	public required string Code { get; set; }

	public required string Symbol { get; set; }

	public required string Name { get; set; }
}
