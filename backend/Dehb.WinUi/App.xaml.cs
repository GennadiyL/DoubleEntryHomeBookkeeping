using System.Runtime.InteropServices;
using Dehb.WinUi.UiSettingsTypes;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace Dehb.WinUi;

public partial class App : Application
{
	private MainWindow? _window;
	public UiSettings UiSettings { get; private set; } = null!;
	public App() => InitializeComponent();
	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		AppInstance instance = AppInstance.FindOrRegisterForKey("DoubleEntryHomeBookkeeping.Local");
		if (!instance.IsCurrent)
		{
			await instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
			Exit();
			return;
		}
		try
		{
			UiSettings = Dehb.WinUi.UiSettingsTypes.UiSettings.Load(Path.Combine(AppContext.BaseDirectory, "uisettings.json"));
		}
		catch (InvalidDataException exception)
		{
			MessageBox(IntPtr.Zero, exception.Message, "Invalid UI settings", 0x10);
			Exit();
			return;
		}
		Dehb.WinUi.Common.Ui.ApplyFontSettings();
		_window = new MainWindow();
		instance.Activated += (_, _) => _window.DispatcherQueue.TryEnqueue(() => _window.ActivateTop());
		_window.Activate();
	}

	[LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int MessageBox(IntPtr owner, string text, string caption, uint type);
}
