using Business.Contracts.Params.Interfaces;

namespace Business.Contracts.Params;

public class CurrencyRateParam
{
	public Guid CurrencyId { get; set; }

	public DateOnly Date { get; set; }

	public decimal Rate { get; set; }

	public string? Comment { get; set; }
}
