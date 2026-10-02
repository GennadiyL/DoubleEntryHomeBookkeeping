using Business.Core.Entities;

namespace Business.Models.Exceptions;

public class InvalidCurrencyCodeException : BaseException
{
	public InvalidCurrencyCodeException(string message) : base(message)
	{
	}
}
