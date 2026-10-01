namespace Business.Contracts.Services.Currencies;

public record CurrencyParam
{
	public required string Code { get; set; }

	public required string Symbol { get; set; }

	public required string Name { get; set; }
}
