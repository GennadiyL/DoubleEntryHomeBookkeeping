using Dehb.WinUi.Catalogs;
using Dehb.WinUi.Common;
using Dehb.WinUi.Currencies;
using Dehb.WinUi.Dialogs;
using Dehb.WinUi.Hosting;
using Dehb.WinUi.Ledger;
using Dehb.WinUi.WindowManagement;
using Dehb.WinUi.UiSettingsTypes;
using Windows.Graphics;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Windows.System;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Xml.Linq;

namespace Dehb.WinUi;

public sealed partial class MainWindow : IDisposable
{
	private readonly Dictionary<string, (Button Button, Image Icon)> _navigation = new();
	private UiSession? _session;
	private readonly Dictionary<string, FrameworkElement> _screens = new();
	private readonly ContentControl _screen = new() { IsTabStop = false, TabFocusNavigation = KeyboardNavigationMode.Cycle, HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
	private readonly TextBlock _error = Ui.Text("");
	public MainWindow()
	{
		InitializeComponent();
		Ui.ApplyPickerFocus(Root);
		Root.Background = Ui.Background;
		Root.Resources["SidebarBackground"] = new SolidColorBrush(Ui.ParseColor(((App)Application.Current).UiSettings.MainWindow.SideBarBackColor));
		WindowPlacement.Attach(this, ((App)Application.Current).UiSettings.MainWindow);
		Start();
		Activated += (_, args) =>
		{
			if (args.WindowActivationState != WindowActivationState.Deactivated &&
				_session?.Coordinator.Top(this) is Window popup && !ReferenceEquals(popup, this))
			{
				popup.Activate();
			}
		};
		Closed += (_, _) => Dispose();
	}
	public void ActivateTop() => (_session?.Coordinator.Top(this) ?? this).Activate();
	public void Dispose() { _session?.Dispose(); _session = null; GC.SuppressFinalize(this); }
	private void Start()
	{
		Dispose();
		if (_error.Parent is Panel previousParent)
		{
			previousParent.Children.Remove(_error);
		}

		try
		{
			_session = new();
			Root.ColumnDefinitions.Clear();
			Root.Children.Clear();
			Root.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(280) });
			Root.ColumnDefinitions.Add(new ColumnDefinition());
			_navigation.Clear();
			StackPanel navigation = new() { Spacing = 5 };
			AddSidebarHeading(navigation, "Daily work", false);
			AddSidebarItem(navigation, "Ledger", true, () => { Show("Ledger"); return Task.CompletedTask; });
			AddSidebarItem(navigation, "Reports", false, () => Task.CompletedTask);
			AddSidebarHeading(navigation, "Catalogs", true);
			foreach (string catalog in new[] { "Accounts", "Correspondents", "Categories", "Projects", "Templates", "Currencies" })
			{
				AddSidebarItem(navigation, catalog, true, () => { Show(catalog); return Task.CompletedTask; });
			}
			AddSidebarHeading(navigation, "Application", true);
			AddSidebarItem(navigation, "Synchronization", false, () => Task.CompletedTask);
			AddSidebarItem(navigation, "Settings", false, () => Task.CompletedTask);
			AddSidebarItem(navigation, "Help", true, async () =>
			{
				UiEditorPopupSettings settings = ((App)Application.Current).UiSettings.HelpPopup;
				EditorWindow help = new(this, _session.Coordinator, "Help", "Help", new SizeInt32(settings.Width, settings.Height));
				Button guide = Ui.Button("User guide", () =>
				{
					string path = Path.Combine(AppContext.BaseDirectory, "Assets", "UserGuide.md");
					if (!File.Exists(path)) { throw new FileNotFoundException("User guide file was not found.", path); }
					System.Diagnostics.ProcessStartInfo start = new("notepad.exe") { UseShellExecute = false };
					start.ArgumentList.Add(path);
					using System.Diagnostics.Process? process = System.Diagnostics.Process.Start(start);
				}, help.Error);
				guide.HorizontalAlignment = HorizontalAlignment.Center;
				EditorToolTips.SetHint(guide, "Open user guide");
				help.Fields.Children.Add(guide);
				TextBlock information = Ui.Text("Double Entry Home Bookkeeping\n" + typeof(App).Assembly.GetName().Version);
				information.HorizontalAlignment = HorizontalAlignment.Center;
				information.TextAlignment = TextAlignment.Center;
				help.Fields.Children.Add(information);
				help.Error.TextAlignment = TextAlignment.Center;
				help.Commands.HorizontalAlignment = HorizontalAlignment.Center;
				Grid.SetColumnSpan(help.Commands, 2);
				Button close = Ui.Button("Close", help.Close, help.Error);
				EditorToolTips.SetHint(close, "Close (Esc)");
				help.Commands.Children.Add(close);
				await help.Show();
			});
			AddSidebarItem(navigation, "Exit", true, () => { Close(); return Task.CompletedTask; });
			navigation.Children.Add(_error);
			Root.Children.Add(new Border
			{
				Background = (Brush)Root.Resources["SidebarBackground"],
				Padding = new Thickness(16, 24, 16, 24),
				Child = new ScrollViewer { IsTabStop = false, Content = navigation, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }
			});
			_screen.Margin = new Thickness(20, 16, 16, 16);
			Grid.SetColumn(_screen, 1);
			Root.Children.Add(_screen);
			Show("Ledger");
		}
		catch (Exception e)
		{
			Root.Children.Clear();
			_error.Text = "Cannot open Local database: " + e.Message;
			StackPanel failure = new() { Spacing = 12 };
			failure.Children.Add(_error);
			failure.Children.Add(Ui.Button("Retry", Start, _error));
			Root.Children.Add(failure);
		}
	}
	private void AddSidebarHeading(StackPanel navigation, string text, bool separate)
	{
		navigation.Children.Add(new TextBlock
		{
			Text = text, FontSize = ((App)Application.Current).UiSettings.MainWindow.SideBarFontSize, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
			Foreground = (Brush)Root.Resources["SidebarHeading"], Margin = new Thickness(4, separate ? 20 : 0, 0, 6)
		});
	}

	private void AddSidebarItem(StackPanel navigation, string name, bool enabled, Func<Task> action)
	{
		Image icon = new()
		{
			Width = 24, Height = 24, Stretch = Stretch.Uniform,
			Source = new SvgImageSource(new Uri($"ms-appx:///Assets/Icons/{name.ToLowerInvariant()}-{(enabled ? "enabled" : "disabled")}.svg"))
		};
		Grid content = new() { ColumnSpacing = 16 };
		content.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(24) });
		content.ColumnDefinitions.Add(new ColumnDefinition());
		content.Children.Add(icon);
		TextBlock label = new() { Text = name, FontSize = ((App)Application.Current).UiSettings.MainWindow.SideBarFontSize, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(label, 1);
		content.Children.Add(label);
		Button button = Ui.AsyncButton(name, action, _error);
		Style sidebarStyle = new(typeof(Button)) { BasedOn = (Style)Root.Resources["SidebarButtonStyle"] };
		Ui.ApplyFocusStyle(sidebarStyle);
		button.Style = sidebarStyle;
		button.IsTabStop = false;
		button.Height = ((App)Application.Current).UiSettings.MainWindow.SideBarItemHeight;
		button.FontSize = ((App)Application.Current).UiSettings.MainWindow.SideBarFontSize;
		button.Content = content;
		button.IsEnabled = enabled;
		button.Foreground = (Brush)Root.Resources[enabled ? "SidebarText" : "SidebarMuted"];
		VirtualKey key = name switch
		{
			"Ledger" => VirtualKey.F12,
			"Reports" => VirtualKey.F11,
			"Accounts" => VirtualKey.F5,
			"Correspondents" => VirtualKey.F6,
			"Categories" => VirtualKey.F7,
			"Projects" => VirtualKey.F8,
			"Templates" => VirtualKey.F9,
			"Currencies" => VirtualKey.F10,
			"Synchronization" => VirtualKey.F3,
			"Settings" => VirtualKey.F2,
			"Help" => VirtualKey.F1,
			"Exit" => VirtualKey.F4,
			_ => throw new ArgumentException("Unknown sidebar action.", nameof(name))
		};
		KeyboardAccelerator shortcut = new() { Key = key, Modifiers = VirtualKeyModifiers.Control };
		shortcut.Invoked += async (_, args) =>
		{
			args.Handled = true;
			if (!button.IsEnabled || _session is null || !ReferenceEquals(_session.Coordinator.Top(this), this))
			{
				return;
			}
			try
			{
				_error.Text = "";
				await action();
			}
			catch (Exception e)
			{
				_error.Text = e.Message;
			}
		};
		button.KeyboardAccelerators.Add(shortcut);
		ToolTipService.SetToolTip(button, $"{name} (Ctrl+{key})");
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetAcceleratorKey(button, $"Ctrl+{key}");
		Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(button, name);
		_navigation.Add(name, (button, icon));
		navigation.Children.Add(button);
	}

	private async void SelectSidebarItem(string name)
	{
		foreach (var item in _navigation)
		{
			bool selected = item.Key == name;
			item.Value.Button.Background = selected ? (Brush)Root.Resources["SidebarSelection"] : null;
			item.Value.Button.Foreground = (Brush)Root.Resources[selected ? "SidebarSelectedText" : item.Value.Button.IsEnabled ? "SidebarText" : "SidebarMuted"];
			item.Value.Icon.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/Icons/{item.Key.ToLowerInvariant()}-{(item.Value.Button.IsEnabled ? "enabled" : "disabled")}.svg"));
		}
		Image selectedIcon = _navigation[name].Icon;
		SvgImageSource selectedSource = new();
		selectedIcon.Source = selectedSource;
		try
		{
			string path = System.IO.Path.Combine(AppContext.BaseDirectory, "Assets", "Icons", name.ToLowerInvariant() + "-enabled.svg");
			XDocument svg = XDocument.Load(path);
			svg.Root!.SetAttributeValue("stroke", "#FFFFFF");
			using MemoryStream stream = new(System.Text.Encoding.UTF8.GetBytes(svg.ToString()));
			SvgImageSourceLoadStatus status = await selectedSource.SetSourceAsync(stream.AsRandomAccessStream());
			if (status != SvgImageSourceLoadStatus.Success)
			{
				throw new InvalidDataException("Could not load the selected sidebar icon.");
			}
		}
		catch (Exception exception)
		{
			_error.Text = exception.Message;
			if (ReferenceEquals(selectedIcon.Source, selectedSource))
			{
				selectedIcon.Source = new SvgImageSource(new Uri($"ms-appx:///Assets/Icons/{name.ToLowerInvariant()}-enabled.svg"));
			}
		}
	}

	private void Show(string name)
	{
		if (_session is null)
		{
			return;
		}

		_session.Coordinator.DockedCatalog = name;
		if (!_screens.TryGetValue(name, out FrameworkElement? view))
		{
			view = name == "Ledger" ? new LedgerView(_session, this) : name == "Currencies" ? new CurrencyView(_session, this) : new CatalogView(_session, this, name, false, false, false);
			_screens[name] = view;
		}
		else if (view is CatalogView catalog)
		{
			catalog.Reload(null, false);
		}
		else if (view is LedgerView ledger)
		{
			ledger.Refresh(null);
		}
		else if (view is CurrencyView currencies)
		{
			currencies.Reload();
		}

		void QueueViewFocus()
		{
			DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (ReferenceEquals(_screen.Content, view) && view.IsLoaded && _session is not null &&
					ReferenceEquals(_session.Coordinator.Top(this), this))
				{
					view.UpdateLayout();
					ViewShortcuts.FocusFirst(view);
				}
			});
		}
		void OnViewLoaded(object sender, RoutedEventArgs args)
		{
			view.Loaded -= OnViewLoaded;
			QueueViewFocus();
		}
		if (!view.IsLoaded)
		{
			view.Loaded += OnViewLoaded;
		}
		_screen.Content = view;
		SelectSidebarItem(name);
		if (view.IsLoaded)
		{
			view.Loaded -= OnViewLoaded;
			QueueViewFocus();
		}
	}
}
