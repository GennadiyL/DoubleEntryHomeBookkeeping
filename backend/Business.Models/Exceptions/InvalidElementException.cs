using Business.Core.Entities;

namespace Business.Models.Exceptions;

public class InvalidElementException : BaseException
{
	public InvalidElementException(string message) : base(message)
	{
	}
}
