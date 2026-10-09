using Business.Contracts.Services;
using Business.Contracts.Services.Currencies;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using WinUi.Controls.DecimalBoxControl;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi;

internal sealed partial class CurrencyView : UserControl
{
	private readonly UiSession _session;
	private readonly Window _owner;
	private readonly TreeGrid _currencies = new() { NameHeader = "Code", IsSearchEnabled = false };
	private readonly ListView _rates = new() { SelectionMode = ListViewSelectionMode.Single };
	private readonly TextBlock _error = Ui.Text("");
	private readonly StackPanel _commands = Ui.Row();
	private CurrencyInfo? Selected => _currencies.SelectedNode?.Data as CurrencyInfo;
	public CurrencyView(UiSession session, Window owner)
	{
		_session = session;
		_owner = owner;
		Ui.StretchRows(_rates);
		Grid grid = new() { ColumnSpacing = 16, RowSpacing = 10 };
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.ColumnDefinitions.Add(new ColumnDefinition());
		grid.Children.Add(_commands);
		Grid.SetColumnSpan(_commands, 2);
		Grid.SetRow(_error, 1);
		Grid.SetColumnSpan(_error, 2);
		grid.Children.Add(_error);
		Grid.SetRow(_currencies, 2);
		grid.Children.Add(_currencies);
		Grid ratePanel = new() { RowSpacing = 8 };
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
		ColumnWidthSettings widths = new();
		try { if (widths.Load("Currencies.Main") is { } savedWidths)
			{
				_currencies.SetColumnWidths(savedWidths);
			}
		} catch (Exception e) { _error.Text = e.Message; }
		_currencies.ColumnWidthsApplying += (_, e) => { try { widths.Save("Currencies.Main", e.Widths.ToArray()); } catch (Exception failure) { e.ErrorMessage = failure.Message; } };
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
				return row;
			}).ToList();
			SetRateActions(true);
		}
		catch (Exception e) { SetRateActions(false); _error.Text = "Rate refresh failed. " + e.Message; }
	}
	private static Grid RateRow(string date, string rate, string description)
	{
		Grid row = new() { ColumnSpacing = 12, HorizontalAlignment = HorizontalAlignment.Stretch };
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		row.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star) });
		string[] values = [date, rate, description];
		for (int i = 0; i < values.Length; i++)
		{
			TextBlock cell = new() { Text = values[i], TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = i == 1 ? TextAlignment.Right : TextAlignment.Left };
			ToolTipService.SetToolTip(cell, values[i]); Grid.SetColumn(cell, i); row.Children.Add(cell);
		}
		return row;
	}
	private async Task EditCurrency(CurrencyInfo? old)
	{
		try
		{
			EditorWindow window = new(_owner, _session.Coordinator, "Currencies.Editor", old is null ? "Add currency" : "Edit currency");
			List<AvailableCurrencyInfo> available = _session.Host.Call<ICurrencyService, List<AvailableCurrencyInfo>>(s => s.GetAvailableCurrencies());
			HashSet<string> existingCodes = _session.Currencies().Select(c => c.Code).ToHashSet();
			ComboBox code = new() { Header = "Code", DisplayMemberPath = "Code", ItemsSource = available.Where(c => !existingCodes.Contains(c.Code)).ToList(), IsEnabled = old is null };
			TextBox name = new() { Header = "Name", Text = old?.Name ?? "" };
			TextBox symbol = new() { Header = "Symbol", Text = old?.Symbol ?? "" };
			CheckBox favorite = new() { Content = "Favorite", IsChecked = old?.IsFavorite ?? false };
			DecimalBox initialRate = new() { Header = "Initial rate", Precision = _session.Settings.RatePrecision, Value = null };
			code.SelectionChanged += (_, _) => { if (code.SelectedItem is AvailableCurrencyInfo info) { name.Text = info.Name; symbol.Text = info.Symbol; } };
			window.Fields.Children.Add(old is null ? code : Ui.Text("Code: " + old.Code));
			window.Fields.Children.Add(name);
			window.Fields.Children.Add(symbol);
			window.Fields.Children.Add(favorite);
			if (old is null)
			{
				window.Fields.Children.Add(initialRate);
			}

			window.Fields.Children.Add(Ui.Button("Restore defaults", () =>
			{
				string? selectedCode = old?.Code ?? (code.SelectedItem as AvailableCurrencyInfo)?.Code;
				var region = System.Globalization.CultureInfo.GetCultures(System.Globalization.CultureTypes.SpecificCultures).Select(c => { try { return new System.Globalization.RegionInfo(c.Name); } catch { return null; } }).FirstOrDefault(r => r?.ISOCurrencySymbol == selectedCode);
				if (region is null)
				{
					throw new InvalidOperationException("Currency defaults unavailable.");
				}

				name.Text = region.CurrencyEnglishName;
				symbol.Text = region.CurrencySymbol;
			}, window.Error));
			string Snapshot() => $"{code.SelectedItem}|{name.Text}|{symbol.Text}|{favorite.IsChecked}|{initialRate.Text}";
			string initial = Snapshot();
			window.IsDirty = () => Snapshot() != initial;
			window.SaveButton(() =>
			{
				CurrencyParam param = new() { Code = old?.Code ?? (code.SelectedItem as AvailableCurrencyInfo)?.Code ?? throw new InvalidOperationException("Select a currency."), Name = name.Text, Symbol = symbol.Text };
				decimal rateValue = old is null ? Ui.Number(initialRate) : 1m;
				bool favoriteValue = favorite.IsChecked == true;
				_session.Host.Atomic<ICurrencyService, Guid>(async service =>
				{
					Guid savedId;
					if (old is null)
					{
						savedId = await service.Add(param, rateValue);
					}
					else
					{ savedId = old.Id; await service.Update(savedId, param); }
					await service.SetFavoriteStatus(savedId, favoriteValue);
					return savedId;
				});
			});
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
