using Business.Core.Entities;

namespace Business.Models.Exceptions;

public class GroupNotFoundException : BaseException
{
	public GroupNotFoundException(string message) : base(message)
	{
	}
}
