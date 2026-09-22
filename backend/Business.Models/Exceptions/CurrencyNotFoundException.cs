using Business.Core.Entities;

namespace Business.Models.Exceptions;

public class CurrencyNotFoundException : BaseException
{
	public CurrencyNotFoundException(string message) : base(message)
	{
	}
}
