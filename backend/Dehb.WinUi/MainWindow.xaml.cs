using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using System.Xml.Linq;

namespace Dehb.WinUi;

public sealed partial class MainWindow : IDisposable
{
	private readonly Dictionary<string, (Button Button, Image Icon)> _navigation = new();
	private UiSession? _session;
	private readonly Dictionary<string, FrameworkElement> _screens = new();
	private readonly ContentControl _screen = new() { HorizontalContentAlignment = HorizontalAlignment.Stretch, VerticalContentAlignment = VerticalAlignment.Stretch };
	private readonly TextBlock _error = Ui.Text("");
	public MainWindow()
	{
		InitializeComponent();
		WindowPlacement.Attach(this);
		Start();
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
				EditorWindow help = new(this, _session.Coordinator, "Help", "Help");
				help.Fields.Children.Add(new Button { Content = "User guide", IsEnabled = false });
				help.Fields.Children.Add(Ui.Text("Double Entry Home Bookkeeping\n" + typeof(App).Assembly.GetName().Version));
				help.Commands.Children.Add(Ui.Button("Close", help.Close, help.Error));
				await help.Show();
			});
			AddSidebarItem(navigation, "Exit", true, () => { Close(); return Task.CompletedTask; });
			navigation.Children.Add(_error);
			Root.Children.Add(new Border
			{
				Background = (Brush)Root.Resources["SidebarBackground"],
				Padding = new Thickness(16, 24, 16, 24),
				Child = new ScrollViewer { Content = navigation, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled, VerticalScrollBarVisibility = ScrollBarVisibility.Auto }
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
			Text = text, FontSize = 15, FontWeight = Microsoft.UI.Text.FontWeights.SemiBold,
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
		TextBlock label = new() { Text = name, FontSize = 17, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(label, 1);
		content.Children.Add(label);
		Button button = Ui.AsyncButton(name, action, _error);
		button.Style = (Style)Root.Resources["SidebarButtonStyle"];
		button.Content = content;
		button.IsEnabled = enabled;
		button.Foreground = (Brush)Root.Resources[enabled ? "SidebarText" : "SidebarMuted"];
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

		_screen.Content = view;
		SelectSidebarItem(name);
	}
}
