using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Principal;
using Microsoft.Win32.SafeHandles;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Records TreeGrid pointer and drag diagnostics for QA reproduction.
/// Creates a separate log for each application process and session.
/// Stores logs beneath the executable directory in the Logs folder.
/// Identifies nodes by process-local identity without recording names or data.
/// Writes each entry immediately so an interrupted session remains useful.
/// Pointer movement is sampled by the presenter to limit disk activity.
/// File access failures are reported to Trace without interrupting input.
/// Does not change pointer handling, selection or drag acceptance.
/// </summary>
internal static partial class TreeGridDragLog
{
	private static readonly object Sync = new();
	private static string? _path;
	private static bool _failed;

	internal static string Node(TreeGridNode? node) => node is null ? "null" : $"{RuntimeHelpers.GetHashCode(node):x}/group={node.IsGroup}/root={node.Parent is null}";

	private static string EnvironmentDetails()
	{
		List<string> details = new() { "DiagnosticsVersion=2" };
		try
		{
			using WindowsIdentity identity = WindowsIdentity.GetCurrent();
			details.Add(GetTokenInformation(identity.AccessToken, 20, out int elevated, sizeof(int), out _)
				? $"TokenElevation={elevated != 0}"
				: $"TokenElevation=unknown; Win32Error={Marshal.GetLastPInvokeError()}");
		}
		catch (Exception exception)
		{
			details.Add($"TokenElevation=unknown; error={exception.GetType().Name}; HResult=0x{exception.HResult:X8}");
		}
		try
		{
			using Process process = Process.GetCurrentProcess();
			ProcessModule[] modules = process.Modules.Cast<ProcessModule>().ToArray();
			foreach (string name in new[] { "Microsoft.UI.Xaml.dll", "Microsoft.UI.Input.dll" })
			{
				ProcessModule[] matches = modules.Where(module => string.Equals(module.ModuleName, name, StringComparison.OrdinalIgnoreCase)).ToArray();
				if (matches.Length == 0)
				{
					details.Add($"Module={name}; not loaded");
				}
				foreach (ProcessModule module in matches)
				{
					details.Add($"Module={name}; path={module.FileName}; fileVersion={module.FileVersionInfo.FileVersion}; productVersion={module.FileVersionInfo.ProductVersion}");
				}
			}
		}
		catch (Exception exception)
		{
			details.Add($"Module inspection failed; error={exception.GetType().Name}; HResult=0x{exception.HResult:X8}");
		}
		return string.Join(Environment.NewLine, details) + Environment.NewLine;
	}

	[LibraryImport("advapi32.dll", SetLastError = true)]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetTokenInformation(SafeAccessTokenHandle token, int informationClass, out int information, int informationLength, out int returnLength);

	internal static void Write(string message)
	{
		lock (Sync)
		{
			if (_failed)
			{
				return;
			}
			try
			{
				if (_path is null)
				{
					string directory = Path.Combine(AppContext.BaseDirectory, "Logs");
					Directory.CreateDirectory(directory);
					_path = Path.Combine(directory, $"drag-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}-{Environment.ProcessId}.log");
					File.AppendAllText(_path, $"UTC session; OS={Environment.OSVersion}; runtime={Environment.Version}; process={Environment.ProcessId}; Xaml={typeof(Microsoft.UI.Xaml.UIElement).Assembly.FullName}{Environment.NewLine}");
					File.AppendAllText(_path, EnvironmentDetails());
				}
				File.AppendAllText(_path, $"{DateTime.UtcNow:O} {message}{Environment.NewLine}");
			}
			catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or System.Security.SecurityException)
			{
				_failed = true;
				Trace.WriteLine($"TreeGrid drag logging failed: {exception}");
			}
		}
	}
}
