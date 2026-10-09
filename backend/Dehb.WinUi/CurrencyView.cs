using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using System.Collections.ObjectModel;
using WinUi.Controls.DecimalBoxControl;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi;

internal sealed partial class CurrencyView : UserControl
{
	private readonly UiSession _session;
	private readonly Window _owner;
	private readonly TreeGrid _currencies = new() { NameHeader = "Code", IsSearchEnabled = false };
	private readonly ListView _rates = new() { SelectionMode = ListViewSelectionMode.Single, Padding = new Thickness(0) };
	private readonly TextBlock _error = Ui.Text("");
	private readonly StackPanel _commands = Ui.Row();
	private CurrencyInfo? Selected => _currencies.SelectedNode?.Data as CurrencyInfo;
	public CurrencyView(UiSession session, Window owner)
	{
		_session = session;
		_owner = owner;
		Ui.StretchRows(_rates);
		_rates.Resources[typeof(ScrollViewer)] = (Style)Application.Current.Resources["PersistentListScrollViewerStyle"];
		ScrollViewer.SetVerticalScrollBarVisibility(_rates, ScrollBarVisibility.Visible);
		Style rateRowStyle = new(typeof(ListViewItem)) { BasedOn = _rates.ItemContainerStyle };
		rateRowStyle.Setters.Add(new Setter(UIElement.UseSystemFocusVisualsProperty, false));
		rateRowStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
		rateRowStyle.Setters.Add(new Setter(Control.BorderThicknessProperty, new Thickness(0)));
		rateRowStyle.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, 28d));
		rateRowStyle.Setters.Add(new Setter(FrameworkElement.HeightProperty, 28d));
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
		Grid ratePanel = new() { RowSpacing = 8, Width = 620, HorizontalAlignment = HorizontalAlignment.Left };
		ratePanel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		ratePanel.RowDefinitions.Add(new RowDefinition());
		ratePanel.Children.Add(RateRow("Date", "Rate", "Description"));
		Grid.SetRow(_rates, 1); ratePanel.Children.Add(_rates);
		Grid.SetRow(ratePanel, 2);
		Grid.SetColumn(ratePanel, 1);
		grid.Children.Add(ratePanel);
		Content = grid;
		_currencies.Columns.Add(new TreeGridColumn { Header = "Name", BindingPath = "Data.Name" });
		_currencies.Columns.Add(new TreeGridColumn { Header = "Symbol", BindingPath = "Data.Symbol" });
		_currencies.SetFixedColumnWidths([120, 120, 80, 80]);
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
		_commands.Children.Add(Ui.Button("Retry", Reload, _error));
		_commands.Children.Add(Ui.AsyncButton("Add currency", () => EditCurrency(null), _error));
		_commands.Children.Add(Ui.AsyncButton("Edit currency", async () => { if (Selected is { } selected)
			{
				await EditCurrency(selected);
			}
		}, _error));
		_commands.Children.Add(Ui.AsyncButton("Delete currency", async () =>
		{
			if (Selected is not { } selected || !await Ui.Confirm(owner, "Delete " + selected.Code + "?"))
			{
				return;
			}

			session.Host.Call<ICurrencyService>(s => s.Delete(selected.Id));
			Reload();
		}, _error));
		_commands.Children.Add(Ui.AsyncButton("Add rate", () => EditRate(null), _error));
		_commands.Children.Add(Ui.AsyncButton("Edit rate", async () => { if ((_rates.SelectedItem as FrameworkElement)?.Tag is CurrencyRateInfo rate)
			{
				await EditRate(rate);
			}
		}, _error));
		_commands.Children.Add(Ui.AsyncButton("Delete rates", DeleteRates, _error));
		Reload();
	}
	public void Reload()
	{
		try
		{
			Guid? id = Selected?.Id;
			_currencies.ItemsSource = new ObservableCollection<TreeGridNode>(_session.Currencies().OrderBy(c => c.Order).Select(c => new TreeGridNode { Name = c.Code, Data = c, IsStarred = c.IsFavorite }));
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
		foreach (Button button in _commands.Children.OfType<Button>().Skip(4))
		{
			button.IsEnabled = enabled;
		}
	}
	private void LoadRates()
	{
		try
		{
			if (Selected is not { } selected)
			{ _rates.ItemsSource = null; SetRateActions(false); return; }
			List<CurrencyRateInfo> rates = _session.Host.Call<ICurrencyRateService, List<CurrencyRateInfo>>(s => s.GetRates(selected.Id));
			_rates.ItemsSource = rates.OrderBy(r => r.IsInitial).ThenByDescending(r => r.Date).Select(r =>
			{
				Grid row = RateRow(r.IsInitial ? "Initial" : r.Date.ToString(System.Globalization.CultureInfo.CurrentCulture), r.Rate.ToString("N" + _session.Settings.RatePrecision, System.Globalization.CultureInfo.CurrentCulture), r.Description ?? "");
				row.Tag = r;
				Border outline = new()
				{
					BorderThickness = new Thickness(1),
					BorderBrush = new SolidColorBrush(Windows.UI.Color.FromArgb(255, 96, 96, 96)),
					IsHitTestVisible = false,
					Visibility = Visibility.Collapsed
				};
				Grid.SetColumnSpan(outline, 3);
				row.Children.Add(outline);
				return row;
			}).ToList();
			SetRateActions(true);
		}
		catch (Exception e) { SetRateActions(false); _error.Text = "Rate refresh failed. " + e.Message; }
	}
	private static Grid RateRow(string date, string rate, string description)
	{
		Grid row = new() { Width = 600, Height = 28, HorizontalAlignment = HorizontalAlignment.Left };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(120) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(360) });
		string[] values = [date, rate, description];
		for (int i = 0; i < values.Length; i++)
		{
			TextBlock cell = new() { Text = values[i], Margin = new Thickness(8, 0, 8, 0), VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = i == 1 ? TextAlignment.Right : TextAlignment.Left };
			ToolTipService.SetToolTip(cell, values[i]); Grid.SetColumn(cell, i); row.Children.Add(cell);
		}
		return row;
	}
	private async Task EditCurrency(CurrencyInfo? old)
	{
		try
		{
			EditorWindow window = new(_owner, _session.Coordinator, "Currencies.Editor", old is null ? "Add currency" : "Edit currency", new Windows.Graphics.SizeInt32(372, old is null ? 400 : 280));
			((Grid)window.Content).Background = (Microsoft.UI.Xaml.Media.Brush)((FrameworkElement)_owner.Content).Resources["SidebarBackground"];
			if (old is null)
			{
				List<AvailableCurrencyInfo> available = _session.Host.Call<ICurrencyService, List<AvailableCurrencyInfo>>(s => s.GetAvailableCurrencies());
				HashSet<string> existingCodes = _session.Currencies().Select(c => c.Code).ToHashSet();
				ComboBox code = new() { Header = "Currency", DisplayMemberPath = "EnglishName", Width = 320, HorizontalAlignment = HorizontalAlignment.Left, ItemsSource = available.Where(c => !existingCodes.Contains(c.Code)).OrderBy(c => c.EnglishName, StringComparer.OrdinalIgnoreCase).ToList() };
				StackPanel metadata = new() { Spacing = 16, Margin = new Thickness(0, 8, 0, 8) };
				TextBlock isoCode = Ui.Text("Code:");
				TextBlock englishName = Ui.Text("English Name:");
				TextBlock symbol = Ui.Text("Symbol:");
				metadata.Children.Add(isoCode);
				metadata.Children.Add(englishName);
				metadata.Children.Add(symbol);
				DecimalBox initialRate = new() { Header = "Initial rate", Width = 320, HorizontalAlignment = HorizontalAlignment.Left, Precision = _session.Settings.RatePrecision, Value = null };
				code.SelectionChanged += (_, _) =>
				{
					if (code.SelectedItem is AvailableCurrencyInfo info)
					{
						isoCode.Text = $"Code: {info.Code}";
						englishName.Text = $"English Name: {info.EnglishName}";
						symbol.Text = $"Symbol: {info.Symbol}";
						ToolTipService.SetToolTip(code, info.EnglishName);
					}
				};
				window.Fields.Children.Add(code);
				window.Fields.Children.Add(metadata);
				window.Fields.Children.Add(initialRate);
				window.IsDirty = () => code.SelectedItem is not null || !string.IsNullOrWhiteSpace(initialRate.Text);
				window.SaveButton(() =>
				{
					string selectedCode = (code.SelectedItem as AvailableCurrencyInfo)?.Code ?? throw new InvalidOperationException("Select a currency.");
					_session.Host.Call<ICurrencyService, Guid>(service => service.Add(selectedCode, Ui.Number(initialRate)));
				});
			}
			else
			{
				TextBox name = new() { Header = "Name (1–6 characters)", Text = old.Name, MaxLength = 6, Width = 320, HorizontalAlignment = HorizontalAlignment.Left };
				window.Fields.Children.Add(name);
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
		if (Selected is not { } currency)
		{
			return;
		}

		EditorWindow window = new(_owner, _session.Coordinator, "Currencies.Rate", "Rate: " + currency.Code);
		CalendarDatePicker date = new() { Date = new DateTimeOffset((old?.Date ?? DateOnly.FromDateTime(DateTime.Today)).ToDateTime(TimeOnly.MinValue)), IsEnabled = old is null };
		DecimalBox rate = new() { Header = "Rate", Precision = _session.Settings.RatePrecision, Value = old?.Rate, IsReadOnly = currency.Id == _session.Settings.BaseCurrencyId };
		if (currency.Id == _session.Settings.BaseCurrencyId)
		{
			rate.Value = 1m;
		}

		TextBox description = new() { Header = "Description", Text = old?.Description ?? "" };
		window.Fields.Children.Add(old?.IsInitial == true ? Ui.Text("Initial") : date);
		window.Fields.Children.Add(rate);
		window.Fields.Children.Add(description);
		string Snapshot() => $"{date.Date}|{rate.Text}|{description.Text}";
		string initial = Snapshot();
		window.IsDirty = () => Snapshot() != initial;
		window.SaveButton(() =>
		{
			CurrencyRateParam param = new() { CurrencyId = currency.Id, Date = old?.IsInitial == true ? old.Date : DateOnly.FromDateTime(date.Date?.DateTime ?? throw new InvalidOperationException("Choose a date.")), Rate = Ui.Number(rate), Description = description.Text };
			_session.Host.Call<ICurrencyRateService, Guid>(s => s.AddOrUpdate(param));
		});
		await window.Show();
		LoadRates();
	}
	private async Task DeleteRates()
	{
		if (Selected is not { } currency)
		{
			return;
		}

		EditorWindow window = new(_owner, _session.Coordinator, "Currencies.DeleteRates", "Delete rates: " + currency.Code);
		CalendarDatePicker from = new() { Header = "From", Date = DateTimeOffset.Now }, to = new() { Header = "To", Date = DateTimeOffset.Now };
		window.Fields.Children.Add(Ui.Row(from, to));
		window.SaveButton(() => _session.Host.Call<ICurrencyRateService>(s => s.Delete(currency.Id,
			DateOnly.FromDateTime(from.Date?.DateTime ?? throw new InvalidOperationException("Choose From.")), DateOnly.FromDateTime(to.Date?.DateTime ?? throw new InvalidOperationException("Choose To.")))));
		await window.Show();
		LoadRates();
	}
}
