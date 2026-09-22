using Shared.Contracts;

namespace Tests.Common.Mocks.Shared;

/// <summary>
/// Defines the logging service test double.
/// Accepts logging operations without emitting entries to an external logging system.
/// Tests construct or derive from it when code requires ILogService.
/// It exposes enabled logging levels and implements the complete shared logging contract.
/// It is test infrastructure and is not registered by production hosts.
/// </summary>
public class MockLogService : ILogService
{
	public virtual void Debug(string message, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Debug(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Debug(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Info(string message, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Info(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Info(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Warn(string message, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Warn(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Warn(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Error(string message, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Error(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Error(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Fatal(string message, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Fatal(string message, Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }
	public virtual void Fatal(Exception exception, string filePath = "", int lineNumber = 0, string memberName = "") { }

	public virtual bool IsDebugEnabled => true;
	public virtual bool IsInfoEnabled => true;
	public virtual bool IsWarnEnabled => true;
	public virtual bool IsErrorEnabled => true;
	public virtual bool IsFatalEnabled => true;
}
