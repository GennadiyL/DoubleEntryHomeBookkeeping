namespace Business.Core.Entities;

/// <summary>
/// Defines the business exception base.
/// Provides a common exception type for failures intentionally exposed by the business layer.
/// Domain-specific exceptions derive from it and select the appropriate constructor.
/// Infrastructure exception handlers can recognize business failures through this shared base.
/// It does not assign transport-specific status codes or perform logging.
/// </summary>
public class BaseException : Exception
{
	public BaseException()
	{
	}

	public BaseException(string? message) : base(message)
	{
	}

	public BaseException(string? message, Exception? innerException) : base(message, innerException)
	{
	}
}
