using Shared.Contracts;

namespace Shared.Impl.Logging;

/// <summary>
/// Defines the null logging service.
/// Provides a no-op ILogService implementation when no logging technology is selected.
/// SharedDiConfiguration registers it by default and hosts may replace that registration.
/// It lets business code depend on logging without null checks or a third-party package.
/// It intentionally stores and emits no log entries.
/// </summary>
internal class NullLogService : ILogService
{
	public void Debug(string message, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Debug(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Debug(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Info(string message, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Info(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Info(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Warn(string message, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Warn(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Warn(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Error(string message, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Error(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Error(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Fatal(string message, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Fatal(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public void Fatal(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "")
	{
	}

	public bool IsDebugEnabled => true;
	public bool IsInfoEnabled => true;
	public bool IsWarnEnabled => true;
	public bool IsErrorEnabled => true;
	public bool IsFatalEnabled => true;
}
