using Business.Core.Entities;

namespace Business.Models.Exceptions;

public class ElementNotFoundException : BaseException
{
	public ElementNotFoundException(string message) : base(message)
	{
	}
}
