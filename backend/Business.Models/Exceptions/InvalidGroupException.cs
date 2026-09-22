using Business.Core.Entities;

namespace Business.Models.Exceptions;

public class InvalidGroupException : BaseException
{
	public InvalidGroupException(string message) : base(message)
	{
	}
}
