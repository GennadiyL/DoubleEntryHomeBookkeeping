using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;

namespace Dehb.WinUi;

public partial class App : Application
{
	private MainWindow? _window;
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
		_window = new MainWindow();
		instance.Activated += (_, _) => _window.DispatcherQueue.TryEnqueue(() => _window.ActivateTop());
		_window.Activate();
	}
}
