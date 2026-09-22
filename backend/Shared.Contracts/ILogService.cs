using System.Runtime.CompilerServices;
#pragma warning disable CA1716

namespace Shared.Contracts;

public interface ILogService
{
	/// <summary>
	/// Performs the Debug operation through the injectable logging service contract.
	/// </summary>
	public void Debug(
		string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Debug operation through the injectable logging service contract.
	/// </summary>
	public void Debug(
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Debug operation through the injectable logging service contract.
	/// </summary>
	public void Debug(
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Info operation through the injectable logging service contract.
	/// </summary>
	public void Info(
		string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Info operation through the injectable logging service contract.
	/// </summary>
	public void Info(
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Info operation through the injectable logging service contract.
	/// </summary>
	public void Info(
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Warn operation through the injectable logging service contract.
	/// </summary>
	public void Warn(
		string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Warn operation through the injectable logging service contract.
	/// </summary>
	public void Warn(
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Warn operation through the injectable logging service contract.
	/// </summary>
	public void Warn(
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Error operation through the injectable logging service contract.
	/// </summary>
	public void Error(
		string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Error operation through the injectable logging service contract.
	/// </summary>
	public void Error(
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Error operation through the injectable logging service contract.
	/// </summary>
	public void Error(
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Fatal operation through the injectable logging service contract.
	/// </summary>
	public void Fatal(
		string message,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Fatal operation through the injectable logging service contract.
	/// </summary>
	public void Fatal(
		string message,
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	/// <summary>
	/// Performs the Fatal operation through the injectable logging service contract.
	/// </summary>
	public void Fatal(
		Exception exception,
		[CallerFilePath] string filePath = "",
		[CallerLineNumber] int lineNumber = 0,
		[CallerMemberName] string memberName = "");

	public bool IsDebugEnabled { get; }
	public bool IsInfoEnabled { get; }
	public bool IsWarnEnabled { get; }
	public bool IsErrorEnabled { get; }
	public bool IsFatalEnabled { get; }
}
