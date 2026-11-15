using System.Runtime.InteropServices;
using Dehb.WinUi.UiSettingsTypes;
using Microsoft.UI.Xaml;
using Microsoft.Windows.AppLifecycle;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Shared.Contracts;
using Shared.Impl;

namespace Dehb.WinUi;

public partial class App : Application
{
	private MainWindow? _window;
	private ServiceProvider? _sharedServices;
	private IServiceScope? _sharedScope;
	public UiSettings UiSettings { get; private set; } = null!;
	public App() => InitializeComponent();
	protected override async void OnLaunched(LaunchActivatedEventArgs args)
	{
		AppInstance instance;
		try
		{
			ISharedContext sharedContext = InitializeSharedContext();
			IConfiguration configuration = _sharedScope!.ServiceProvider.GetRequiredService<IConfiguration>();
			string directory = Path.GetFullPath(configuration[Business.Models.Constants.StorageValues.LocalDirectoryConfigurationKey]
				?? throw new InvalidDataException("DataAccess:LocalDirectory is required."), AppContext.BaseDirectory);
			string localPath = Path.Combine(directory, Business.Models.Constants.StorageValues.LocalFileName).ToUpperInvariant();
			string instanceKey = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(localPath)));
			instance = AppInstance.FindOrRegisterForKey("DoubleEntryHomeBookkeeping.Local." + instanceKey);
			if (!instance.IsCurrent)
			{
				await instance.RedirectActivationToAsync(AppInstance.GetCurrent().GetActivatedEventArgs());
				DisposeSharedContext();
				Exit();
				return;
			}
			UiSettings = UiSettingsLoader.Load(sharedContext.FileService, Path.Combine(AppContext.BaseDirectory, "Assets", "uisettings.json"));
		}
		catch (InvalidDataException exception)
		{
			MessageBox(IntPtr.Zero, exception.Message, "Invalid UI settings", 0x10);
			DisposeSharedContext();
			Exit();
			return;
		}
		Dehb.WinUi.Common.Ui.ApplyFontSettings();
		_window = new MainWindow(_sharedScope!.ServiceProvider.GetRequiredService<ISharedContext>());
		_window.Closed += (_, _) => DisposeSharedContext();
		instance.Activated += (_, _) => _window.DispatcherQueue.TryEnqueue(() => _window.ActivateTop());
		_window.Activate();
	}

	private ISharedContext InitializeSharedContext()
	{
		ServiceCollection services = new();
		services.AddSingleton<IConfiguration>(_ => new ConfigurationBuilder().SetBasePath(AppContext.BaseDirectory)
			.AddJsonFile("appsettings.json", false).Build());
		services.AddSharedModule();
		_sharedServices = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true, ValidateOnBuild = true });
		_sharedScope = _sharedServices.CreateScope();
		return _sharedScope.ServiceProvider.GetRequiredService<ISharedContext>();
	}

	private void DisposeSharedContext()
	{
		_sharedScope?.Dispose();
		_sharedServices?.Dispose();
		_sharedScope = null;
		_sharedServices = null;
	}

	[LibraryImport("user32.dll", EntryPoint = "MessageBoxW", StringMarshalling = StringMarshalling.Utf16)]
	private static partial int MessageBox(IntPtr owner, string text, string caption, uint type);
}
