using Dehb.WinUi.Common;
using Dehb.WinUi.Dialogs;
using Dehb.WinUi.Hosting;
using Dehb.WinUi.UiSettingsTypes;
using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml.Automation;
using System.Collections.ObjectModel;
using WinUi.Controls.DecimalBoxControl;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi.Currencies;

internal sealed partial class CurrencyView : UserControl
{
	private readonly UiSession _session;
	private readonly Window _owner;
	private readonly double[] _rateColumnWidths;
	private readonly TreeGrid _currencies = new() { NameHeader = "Code", IsSearchEnabled = false, FontSize = Ui.FontSize, RowHeight = Ui.RowHeight, SelectionBorderBrush = Ui.SelectionBorder, SelectionBorderThickness = Ui.SelectionBorderThickness };
	private readonly ListView _rates = new() { SelectionMode = ListViewSelectionMode.Single, Padding = new Thickness(0) };
	private readonly TextBlock _error = Ui.Text("");
	private readonly StackPanel _commands = Ui.Row();
	private CurrencyInfo? Selected => _currencies.SelectedNode?.Data as CurrencyInfo;
	public CurrencyView(UiSession session, Window owner)
	{
		_session = session;
		_owner = owner;
		UiSettings settings = ((App)Application.Current).UiSettings;
		_rateColumnWidths = [settings.CurrencyRateGrid.Columns["date"], settings.CurrencyRateGrid.Columns["rate"], settings.CurrencyRateGrid.Columns["description"]];
		Ui.StretchRows(_rates);
		_rates.Resources[typeof(ScrollViewer)] = (Style)Application.Current.Resources["PersistentListScrollViewerStyle"];
		ScrollViewer.SetVerticalScrollBarVisibility(_rates, ScrollBarVisibility.Visible);
		Style rateRowStyle = new(typeof(ListViewItem)) { BasedOn = _rates.ItemContainerStyle };
		rateRowStyle.Setters.Add(new Setter(UIElement.UseSystemFocusVisualsProperty, false));
		rateRowStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
		rateRowStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
		_rates.ItemContainerStyle = rateRowStyle;
		_rates.SelectionChanged += (_, e) =>
		{
			foreach (Grid row in e.RemovedItems.Concat(e.AddedItems).OfType<Grid>())
			{
				foreach (Border outline in row.Children.OfType<Border>())
				{
					outline.Visibility = ReferenceEquals(_rates.SelectedItem, row) ? Visibility.Visible : Visibility.Collapsed;
				}
			}
		};
		Grid grid = new() { ColumnSpacing = 16, RowSpacing = 10 };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.Children.Add(_commands);
		Grid.SetColumnSpan(_commands, 2);
		Grid.SetRow(_error, 1);
		Grid.SetColumnSpan(_error, 2);
		grid.Children.Add(_error);
		Grid.SetRow(_currencies, 2);
		grid.Children.Add(_currencies);
		Grid ratePanel = new() { RowSpacing = 8, Width = _rateColumnWidths.Sum() + 20, HorizontalAlignment = HorizontalAlignment.Left };
		ratePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		ratePanel.RowDefinitions.Add(new RowDefinition());
		ratePanel.Children.Add(RateRow("Date", "Rate", "Description", true));
		Grid.SetRow(_rates, 1); ratePanel.Children.Add(_rates);
		Grid.SetRow(ratePanel, 2);
		Grid.SetColumn(ratePanel, 1);
		grid.Children.Add(ratePanel);
		Content = grid;
		bool ratesFocusedLast = false;
		_rates.GotFocus += (_, _) => ratesFocusedLast = true;
		_currencies.GotFocus += (_, _) => ratesFocusedLast = false;
		ViewShortcuts.Attach(this, () =>
		{
			if (ratesFocusedLast)
			{
				_currencies.FocusRows();
			}
			else
			{
				ViewShortcuts.FocusRows(_rates);
			}
		}, _currencies);
		_currencies.Columns.Add(new TreeGridColumn { Header = "Name", BindingPath = "Data.Name" });
		_currencies.Columns.Add(new TreeGridColumn { Header = "Symbol", BindingPath = "Data.Symbol" });
		_currencies.SetFixedColumnWidths([settings.CurrencyGrid.Columns["code"], settings.CurrencyGrid.Columns["name"], settings.CurrencyGrid.Columns["symbol"], settings.CurrencyGrid.Columns["star"]]);
		_currencies.SelectedNodeChanged += (_, _) => LoadRates();
		_currencies.RowActivated += async (_, _) => await EditCurrency(Selected);
		_currencies.StarChanged += (_, node) =>
		{
			try
			{ session.Host.Call<ICurrencyService>(s => s.SetFavoriteStatus(((CurrencyInfo)node.Data!).Id, node.IsStarred)); }
			catch (Exception e) { node.IsStarred = !node.IsStarred; _error.Text = e.Message; }
		};
		_rates.DoubleTapped += async (_, _) => { try { if ((_rates.SelectedItem as FrameworkElement)?.Tag is CurrencyRateInfo rate)
			{
				await EditRate(rate);
			}
		} catch (Exception e) { _error.Text = e.Message; } };
		_commands.Children.Add(Ui.Button("Refresh", Reload, _error));
		_commands.Children.Add(Ui.AsyncButton("Add currency", () => EditCurrency(null), _error));
		_commands.Children.Add(Ui.AsyncButton("Edit currency", async () => { if (Selected is { } selected)
			{
				await EditCurrency(selected);
			}
		}, _error));
		_commands.Children.Add(Ui.AsyncButton("Delete currency", async () =>
		{
			if (Selected is not { } selected || selected.Id == _session.Settings.BaseCurrencyId || !await Ui.Confirm(owner, "Delete " + selected.Code + "?"))
			{
				return;
			}

			session.Host.Call<ICurrencyService>(s => s.Delete(selected.Id));
			Reload();
		}, _error));
		_commands.Children.Add(Ui.AsyncButton("Add currency rate", () => EditRate(null), _error));
		_commands.Children.Add(Ui.AsyncButton("Edit currency rate", async () => { if ((_rates.SelectedItem as FrameworkElement)?.Tag is CurrencyRateInfo rate)
			{
				await EditRate(rate);
			}
		}, _error));
		_commands.Children.Add(Ui.AsyncButton("Delete currency rates", DeleteRates, _error));
		ConfigureToolbar();
		GridCommands.Attach(_currencies, () => _commands.Children.OfType<Button>(), "Edit currency", true, scope: this);
		GridCommands.Attach(_rates, () => _commands.Children.OfType<Button>(), "Edit currency rate", true, scope: this);
		_currencies.ContextMenuRequested += (_, args) =>
		{
			foreach (Button button in _commands.Children.OfType<Button>()) { args.Menu.Items.Add(GridCommands.MenuItem(button)); }
			GridCommands.AddStarMenu(args.Menu, _currencies, _currencies.IsEnabled);
		};
		GridCommands.AttachMenu(_rates, () => _commands.Children.OfType<Button>());
		Reload();
	}
	private void ConfigureToolbar()
	{
		string[] icons = ["refresh", "add-currency", "edit-currency", "delete-currency", "add-rate", "edit-rate", "delete-rates"];
		Button[] buttons = _commands.Children.OfType<Button>().ToArray();
		for (int i = 0; i < buttons.Length; i++)
		{
			Button button = buttons[i];
			string label = (string)button.Content;
			SvgImageSource enabled = new(new Uri($"ms-appx:///Assets/Icons/{icons[i]}-enabled.svg"));
			SvgImageSource disabled = new(new Uri($"ms-appx:///Assets/Icons/{icons[i]}-disabled.svg"));
			Image icon = new() { Width = 24, Height = 24, Stretch = Stretch.Uniform, Source = button.IsEnabled ? enabled : disabled };
			button.Width = button.Height = 40;
			button.MinWidth = button.MinHeight = 40;
			button.Padding = new Thickness(6);
			button.HorizontalContentAlignment = HorizontalAlignment.Center;
			button.VerticalContentAlignment = VerticalAlignment.Center;
			button.Content = icon;
			ToolTipService.SetToolTip(button, GridCommands.Hint(label));
			AutomationProperties.SetName(button, label);
			button.RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => icon.Source = button.IsEnabled ? enabled : disabled);
		}
		foreach (int index in new[] { 1, 5 })
		{
			_commands.Children.Insert(index, new Border
			{
				Width = 1, Height = 28, Margin = new Thickness(4, 0, 4, 0),
				VerticalAlignment = VerticalAlignment.Center,
				Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
			});
		}
	}
	public void Reload()
	{
		try
		{
			Guid? id = Selected?.Id;
			_currencies.ItemsSource = new ObservableCollection<TreeGridNode>(_session.Currencies().OrderBy(c => c.Order).Select(c => new TreeGridNode { Name = c.Id == _session.Settings.BaseCurrencyId ? $"{c.Code} (✦)" : c.Code, Data = c, IsStarred = c.IsFavorite }));
			_currencies.SelectedNode = _currencies.ItemsSource.FirstOrDefault(n => ((CurrencyInfo)n.Data!).Id == id) ?? _currencies.ItemsSource.FirstOrDefault();
			_error.Text = "";
			_currencies.IsEnabled = true;
			foreach (Button command in _commands.Children.OfType<Button>())
			{
				command.IsEnabled = true;
			}

			LoadRates();
		}
		catch (Exception e) { _error.Text = "Refresh failed. " + e.Message; _currencies.IsEnabled = false; foreach (Button command in _commands.Children.OfType<Button>().Skip(1))
			{
				command.IsEnabled = false;
			}
		}
	}
	private void SetRateActions(bool enabled)
	{
		_rates.IsEnabled = enabled;
		bool canModify = Selected is { } selected && selected.Id != _session.Settings.BaseCurrencyId;
		if (_commands.Children.OfType<Button>().ElementAtOrDefault(3) is Button deleteCurrency)
		{
			deleteCurrency.IsEnabled = canModify;
		}
		foreach (Button button in _commands.Children.OfType<Button>().Skip(4))
		{
			button.IsEnabled = enabled && canModify;
		}
	}
	private void LoadRates()
	{
		try
		{
			if (Selected is not { } selected)
			{ _rates.ItemsSource = null; SetRateActions(false); return; }
			CurrencyRateInfo? previous = (_rates.SelectedItem as FrameworkElement)?.Tag as CurrencyRateInfo;
			List<CurrencyRateInfo> rates = _session.Host.Call<ICurrencyRateService, List<CurrencyRateInfo>>(s => s.GetRates(selected.Id));
			_rates.ItemsSource = rates.OrderBy(r => r.IsInitial).ThenByDescending(r => r.Date).Select(r =>
			{
				Grid row = RateRow(r.IsInitial ? "Initial" : r.Date.ToString(System.Globalization.CultureInfo.CurrentCulture), r.Rate.ToString("N" + _session.Settings.RatePrecision, System.Globalization.CultureInfo.CurrentCulture), r.Description ?? "");
				row.Tag = r;
				Border outline = new()
				{
					BorderThickness = Ui.SelectionBorderThickness,
					BorderBrush = Ui.SelectionBorder,
					IsHitTestVisible = false,
					Visibility = Visibility.Collapsed
				};
				Grid.SetColumnSpan(outline, 3);
				row.Children.Add(outline);
				return row;
			}).ToList();
			_rates.SelectedItem = _rates.Items.OfType<Grid>().FirstOrDefault(row =>
				row.Tag is CurrencyRateInfo rate && previous is not null &&
				rate.CurrencyId == previous.CurrencyId && rate.Date == previous.Date) ?? _rates.Items.FirstOrDefault();
			SetRateActions(true);
		}
		catch (Exception e) { SetRateActions(false); _error.Text = "Rate refresh failed. " + e.Message; }
	}
	private Grid RateRow(string date, string rate, string description, bool header = false)
	{
		Grid row = new() { Width = _rateColumnWidths.Sum(), Height = Ui.RowHeight, HorizontalAlignment = HorizontalAlignment.Left };
		foreach (double width in _rateColumnWidths)
		{
			row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
		}
		string[] values = [date, rate, description];
		for (int i = 0; i < values.Length; i++)
		{
			TextBlock cell = new() { Text = values[i], Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = header ? TextAlignment.Center : i == 1 ? TextAlignment.Right : TextAlignment.Left };
			Grid.SetColumn(cell, i); row.Children.Add(cell);
		}
		return row;
	}
	private async Task EditCurrency(CurrencyInfo? old)
	{
		try
		{
			UiSettings settings = ((App)Application.Current).UiSettings;
			UiEditorPopupSettings popup = old is null ? settings.AddCurrencyEditor : settings.EditCurrencyEditor;
			using EditorWindow window = new(_owner, _session.Coordinator, "Currencies.Editor", old is null ? "Add currency" : "Edit currency", new Windows.Graphics.SizeInt32(popup.Width, popup.Height));
			if (old is null)
			{
				List<AvailableCurrencyInfo> available = _session.Host.Call<ICurrencyService, List<AvailableCurrencyInfo>>(s => s.GetAvailableCurrencies());
				HashSet<string> existingCodes = _session.Currencies().Select(c => c.Code).ToHashSet();
				ComboBox code = new() { DisplayMemberPath = "EnglishName", MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch, ItemsSource = available.Where(c => !existingCodes.Contains(c.Code)).OrderBy(c => c.EnglishName, StringComparer.OrdinalIgnoreCase).ToList() };
				StackPanel metadata = new() { Spacing = 16, Margin = new Thickness(0, 8, 0, 8) };
				TextBlock isoCode = Ui.Text("Code:");
				TextBlock englishName = Ui.Text("English Name:");
				TextBlock symbol = Ui.Text("Symbol:");
				metadata.Children.Add(isoCode);
				metadata.Children.Add(englishName);
				metadata.Children.Add(symbol);
				DecimalBox initialRate = new() { MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch, Precision = _session.Settings.RatePrecision, Value = null };
				code.SelectionChanged += (_, _) =>
				{
					if (code.SelectedItem is AvailableCurrencyInfo info)
					{
						isoCode.Text = $"Code: {info.Code}";
						englishName.Text = $"English Name: {info.EnglishName}";
						symbol.Text = $"Symbol: {info.Symbol}";
					}
				};
				window.Fields.Children.Add(Ui.EditorField("Currency", code));
				window.Fields.Children.Add(metadata);
				window.Fields.Children.Add(Ui.EditorField("Initial rate", initialRate));
				window.IsDirty = () => code.SelectedItem is not null || !string.IsNullOrWhiteSpace(initialRate.Text);
				window.SaveButton(() =>
				{
					string selectedCode = (code.SelectedItem as AvailableCurrencyInfo)?.Code ?? throw new InvalidOperationException("Select a currency.");
					_session.Host.Call<ICurrencyService, Guid>(service => service.Add(selectedCode, Ui.Number(initialRate)));
				});
			}
			else
			{
				TextBox name = new() { Text = old.Name, MaxLength = 6, MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
				window.Fields.Children.Add(Ui.EditorField("Name (1–6 characters)", name));
				window.IsDirty = () => name.Text != old.Name;
				window.SaveButton(() => _session.Host.Call<ICurrencyService>(service => service.Update(old.Id, new CurrencyParam { Name = name.Text })));
			}
			await window.Show();
			Reload();
		}
		catch (Exception e) { _error.Text = e.Message; }
	}
	private async Task EditRate(CurrencyRateInfo? old)
	{
		if (Selected is not { } currency || currency.Id == _session.Settings.BaseCurrencyId)
		{
			return;
		}

		UiSettings settings = ((App)Application.Current).UiSettings;
		UiEditorPopupSettings popup = settings.CurrencyRateEditor;
		using EditorWindow window = new(_owner, _session.Coordinator, "Currencies.Rate", "Rate: " + currency.Code, new Windows.Graphics.SizeInt32(popup.Width, popup.Height));
		CalendarDatePicker? date = null;
		FrameworkElement dateControl;
		if (old is null)
		{
			date = new() { MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch, Date = DateTimeOffset.Now };
			dateControl = date;
		}
		else
		{
			dateControl = new TextBox
			{
				Text = old.IsInitial ? "Initial" : old.Date.ToString(System.Globalization.CultureInfo.CurrentCulture),
				IsReadOnly = true, MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch
			};
		}
		DecimalBox rate = new() { Precision = _session.Settings.RatePrecision, Value = old?.Rate };

		TextBox description = new() { Text = old?.Description ?? "" };
		window.Fields.Children.Add(Ui.EditorField("Date", dateControl));
		window.Fields.Children.Add(Ui.EditorField("Rate", rate));
		window.Fields.Children.Add(Ui.EditorField("Description", description));
		string Snapshot() => $"{old?.Date}|{date?.Date}|{rate.Text}|{description.Text}";
		string initial = Snapshot();
		window.IsDirty = () => Snapshot() != initial;
		window.SaveButton(() =>
		{
			CurrencyRateParam param = new() { CurrencyId = currency.Id, Date = old?.Date ?? DateOnly.FromDateTime(date?.Date?.DateTime ?? throw new InvalidOperationException("Choose a date.")), Rate = Ui.Number(rate), Description = description.Text };
			_session.Host.Call<ICurrencyRateService, Guid>(s => s.AddOrUpdate(param));
		});
		await window.Show();
		LoadRates();
	}
	private async Task DeleteRates()
	{
		if (Selected is not { } currency || currency.Id == _session.Settings.BaseCurrencyId)
		{
			return;
		}

		UiEditorPopupSettings popup = ((App)Application.Current).UiSettings.DeleteCurrencyRateEditor;
		using EditorWindow window = new(_owner, _session.Coordinator, "Currencies.DeleteRates", "Delete rates: " + currency.Code, new Windows.Graphics.SizeInt32(popup.Width, popup.Height));
		CalendarDatePicker from = new() { MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch, Date = DateTimeOffset.Now }, to = new() { MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch, Date = DateTimeOffset.Now };
		window.Fields.Children.Add(Ui.EditorField("From", from));
		window.Fields.Children.Add(Ui.EditorField("To", to));
		window.SaveButton(() => _session.Host.Call<ICurrencyRateService>(s => s.Delete(currency.Id,
			DateOnly.FromDateTime(from.Date?.DateTime ?? throw new InvalidOperationException("Choose From.")), DateOnly.FromDateTime(to.Date?.DateTime ?? throw new InvalidOperationException("Choose To.")))));
		await window.Show();
		LoadRates();
	}
}
