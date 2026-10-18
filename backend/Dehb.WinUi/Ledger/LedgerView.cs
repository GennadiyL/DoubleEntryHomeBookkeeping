using Dehb.WinUi.Common;
using Dehb.WinUi.Hosting;
using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Templates;
using Business.Contracts.Services.Transactions;
using Business.Models.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;

namespace Dehb.WinUi.Ledger;

internal sealed partial class LedgerView : UserControl
{
	private static readonly string[] Headers = ["Date/time", "Description", "State", "Amount", "Cumulative", "Currency"];
	private readonly UiSession _session;
	private readonly Window _owner;
	private readonly CalendarDatePicker _date = new() { Date = DateTimeOffset.Now, MinWidth = 0, VerticalAlignment = VerticalAlignment.Center };
	private readonly ComboBox _step = new() { ItemsSource = new[] { "Week", "Month", "Quarter", "Year" }, SelectedIndex = 1, MinWidth = 0, VerticalAlignment = VerticalAlignment.Center };
	private readonly ListView _list = new() { SelectionMode = ListViewSelectionMode.Single };
	private readonly TextBlock _error = Ui.Text("");
	private readonly TextBlock _notice = Ui.Text("");
	private readonly TextBox _account = new() { IsReadOnly = true, Text = "All accounts", MinWidth = 0, MinHeight = 0, VerticalAlignment = VerticalAlignment.Center };
	private readonly StackPanel _actions = Ui.Row();
	private readonly Grid _header;
	private List<TransactionInfo> _transactions = new();
	private readonly QuickAccountFilters _quickAccounts = new();
	private readonly Button[] _quickAccountButtons = new Button[QuickAccountFilters.Count];
	private Guid? _accountId;
	private bool _ready;
	private DateOnly Day => DateOnly.FromDateTime(_date.Date?.DateTime ?? DateTime.Today);
	private TransactionInfo? Selected => (_list.SelectedItem as Grid)?.Tag as TransactionInfo;
	public LedgerView(UiSession session, Window owner)
	{
		_session = session;
		_owner = owner;
		UiSettingsTypes.UiTransactionGridSettings settings = ((App)Application.Current).UiSettings.TransactionGrid;
		_date.Width = settings.DateWidth;
		_step.Width = settings.RangeWidth;
		_account.Width = settings.AccountWidth;
		Ui.StretchRows(_list);
		_list.Padding = new Thickness(0);
		Style rowStyle = new(typeof(ListViewItem)) { BasedOn = _list.ItemContainerStyle };
		rowStyle.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(0)));
		_list.ItemContainerStyle = rowStyle;
		Button chooseAccount = Ui.AsyncButton("...", async () =>
		{
			Guid? chosen = await session.Pick(owner, "Accounts", false, _accountId, _ => true);
			if (chosen.HasValue)
			{
				AccountInfo account = _session.Host.Call<IAccountService, AccountInfo>(s => s.GetById(chosen.Value));
				_quickAccounts.Remember(account.Id, account.Name);
				UpdateQuickAccounts();
				_accountId = account.Id;
				Reload();
			}
			else
			{
				Refresh(null);
			}
		}, _error);
		Grid layout = new() { RowSpacing = 10 };
		for (int i = 0; i < 6; i++)
		{
			layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}

		layout.RowDefinitions.Add(new RowDefinition());
		layout.Children.Add(_actions);
		Button today = IconButton(Ui.Button("Today", () => _date.Date = DateTimeOffset.Now, _error), "today");
		Button previous = IconButton(Ui.Button("Previous", () => Navigate(-1), _error), "previous");
		Button next = IconButton(Ui.Button("Next", () => Navigate(1), _error), "next");
		StackPanel navigation = Ui.Row(_date, today, Separator(), _step, previous, next);
		Grid.SetRow(navigation, 1);
		layout.Children.Add(navigation);
		Button clearAccount = Ui.Button("x", () => { _accountId = null; Reload(); }, _error);
		IconButton(chooseAccount, "select");
		IconButton(clearAccount, "clear");
		chooseAccount.VerticalAlignment = VerticalAlignment.Center;
		clearAccount.VerticalAlignment = VerticalAlignment.Center;
		_date.SizeChanged += (_, args) => _account.Height = args.NewSize.Height;
		AutomationProperties.SetName(_account, "Account");
		AutomationProperties.SetName(chooseAccount, "Choose account");
		AutomationProperties.SetName(clearAccount, "Clear account");
		ToolTipService.SetToolTip(chooseAccount, "Choose account");
		ToolTipService.SetToolTip(clearAccount, "Clear account");
		AutomationProperties.SetName(_date, "Date");
		AutomationProperties.SetName(_step, "Period");
		StackPanel accountFilter = Ui.Row(_account, chooseAccount, clearAccount, Separator());
		for (int i = 0; i < QuickAccountFilters.Count; i++)
		{
			int slot = i;
			Button button = IconButton(Ui.Button($"Account slot {slot + 1}", () => ApplyQuickAccount(slot), _error), $"digit-{slot + 1}");
			button.VerticalAlignment = VerticalAlignment.Center;
			_quickAccountButtons[slot] = button;
			accountFilter.Children.Add(button);
		}
		UpdateQuickAccounts();
		Grid.SetRow(accountFilter, 2);
		layout.Children.Add(accountFilter);
		Grid.SetRow(_notice, 3);
		layout.Children.Add(_notice);
		Grid.SetRow(_error, 4);
		layout.Children.Add(_error);
		_header = Row(Headers, false, true);
		Grid.SetRow(_header, 5);
		layout.Children.Add(_header);
		Grid.SetRow(_list, 6);
		layout.Children.Add(_list);
		Content = layout;
		_list.DoubleTapped += async (_, _) => await Edit();
		_list.SelectionChanged += (_, _) => Actions();
		_date.DateChanged += (_, _) => Reload();
		Actions();
		Reload();
	}
	private void UpdateQuickAccounts()
	{
		for (int i = 0; i < QuickAccountFilters.Count; i++)
		{
			Button button = _quickAccountButtons[i];
			button.IsEnabled = _quickAccounts.GetId(i).HasValue;
			string label = _quickAccounts.GetName(i) ?? $"Empty account slot {i + 1}";
			ToolTipService.SetToolTip(button, label);
			AutomationProperties.SetName(button, $"Account slot {i + 1}: {label}");
		}
	}

	private void ApplyQuickAccount(int slot)
	{
		if (_quickAccounts.GetId(slot) is not Guid id)
		{
			return;
		}
		AccountTreeInfo tree = _session.Host.Call<IAccountGroupService, AccountTreeInfo>(s => s.GetAccountsTree());
		AccountElementInfo? account = tree.Elements.SingleOrDefault(a => a.Id == id);
		if (account is null)
		{
			string name = _quickAccounts.GetName(slot) ?? "";
			_quickAccounts.Clear(slot);
			UpdateQuickAccounts();
			if (_accountId == id)
			{
				_accountId = null;
				Reload();
			}
			_error.Text = $"Account '{name}' no longer exists. Quick filter slot {slot + 1} was cleared.";
			return;
		}
		_quickAccounts.Remember(id, account.Name);
		UpdateQuickAccounts();
		_accountId = id;
		Reload();
	}

	private void Navigate(int direction)
	{
		DateTime value = Day.ToDateTime(TimeOnly.MinValue);
		_date.Date = new DateTimeOffset(_step.SelectedIndex switch { 0 => value.AddDays(7 * direction), 1 => value.AddMonths(direction), 2 => value.AddMonths(3 * direction), _ => value.AddYears(direction) });
	}
	private void Actions()
	{
		_actions.Children.Clear();
		_actions.Children.Add(Ui.Button("Refresh", Reload, _error));
		_actions.Children.Add(Ui.AsyncButton("Add", async () => { Guid? saved = await _session.EditTransaction(_owner, null, _accountId, null); Refresh(saved); }, _error));
		Button edit = Ui.AsyncButton("Edit", Edit, _error);
		edit.IsEnabled = Selected is not null;
		_actions.Children.Add(edit);
		Button duplicate = Ui.AsyncButton("Duplicate", async () =>
		{
			if (Selected is not { } selected)
			{
				return;
			}

			DuplicateTransactionInfo copy = _session.Host.Call<ITransactionService, DuplicateTransactionInfo>(s => s.DuplicateTransaction(selected.Id));
			TransactionParam param = new() { DateTime = copy.DateTime, Description = copy.Description };
			param.Entries.AddRange(copy.Entries.Select(e => new TransactionEntryParam { AccountId = e.AccountId, Amount = e.Amount, Rate = e.Rate }));
			Refresh(await _session.EditTransaction(_owner, null, null, param));
		}, _error);
		duplicate.IsEnabled = Selected is not null;
		_actions.Children.Add(duplicate);
		_actions.Children.Add(Ui.AsyncButton("From template", async () =>
		{
			Guid? template = await _session.Pick(_owner, "Templates", false, null, _ => true);
			if (template is null)
			{ Refresh(null); return; }
			ApplyTemplateInfo copy = _session.Host.Call<ITemplateService, ApplyTemplateInfo>(s => s.ApplyTemplate(template.Value));
			TransactionParam param = new() { DateTime = copy.DateTime, Description = copy.Description };
			param.Entries.AddRange(copy.Entries.Select(e => new TransactionEntryParam { AccountId = e.AccountId, Amount = e.Amount, Rate = e.Rate }));
			Refresh(await _session.EditTransaction(_owner, null, null, param));
		}, _error));
		Button createTemplate = Ui.AsyncButton("Create template", async () =>
		{
			if (Selected is not { } selected)
			{
				return;
			}

			FromTransactionInfo copy = _session.Host.Call<ITemplateService, FromTransactionInfo>(s => s.FromTransaction(selected.Id));
			await _session.EditTemplate(_owner, null, null, copy);
			Refresh(null);
		}, _error);
		createTemplate.IsEnabled = Selected is not null;
		_actions.Children.Add(createTemplate);
		Button delete = Ui.AsyncButton("Delete", async () =>
		{
			if (Selected is not { } selected || !await Ui.Confirm(_owner, "Delete selected transaction?"))
			{
				return;
			}

			_session.Host.Call<ITransactionService>(s => s.Delete(selected.Id));
			Refresh(null);
		}, _error);
		delete.IsEnabled = Selected is not null;
		_actions.Children.Insert(3, delete);
		if (!_ready)
		{
			foreach (Button button in _actions.Children.OfType<Button>().Skip(1))
			{
				button.IsEnabled = false;
			}
		}
		foreach (Button button in _actions.Children.OfType<Button>().ToArray())
		{
			string label = (string)button.Content;
			string icon = label switch
			{
				"From template" => "from-template",
				"Create template" => "add-template",
				_ => label.ToLowerInvariant()
			};
			if (label is "Add" or "Duplicate")
			{
				_actions.Children.Insert(_actions.Children.IndexOf(button), Separator());
			}
			IconButton(button, icon);
		}
	}
	private static Border Separator() => new()
	{
		Width = 1, Height = 28, Margin = new Thickness(4, 0, 4, 0),
		VerticalAlignment = VerticalAlignment.Center,
		Background = (Brush)Application.Current.Resources["DividerStrokeColorDefaultBrush"]
	};
	private static Button IconButton(Button button, string iconName)
	{
		string label = (string)button.Content;
		SvgImageSource enabled = new(new Uri($"ms-appx:///Assets/Icons/{iconName}-enabled.svg"));
		SvgImageSource disabled = new(new Uri($"ms-appx:///Assets/Icons/{iconName}-disabled.svg"));
		Image icon = new() { Width = 24, Height = 24, Stretch = Stretch.Uniform, Source = button.IsEnabled ? enabled : disabled };
		button.Width = button.Height = 40;
		button.MinWidth = button.MinHeight = 40;
		button.Padding = new Thickness(6);
		button.HorizontalContentAlignment = HorizontalAlignment.Center;
		button.VerticalContentAlignment = VerticalAlignment.Center;
		button.Content = icon;
		ToolTipService.SetToolTip(button, label);
		AutomationProperties.SetName(button, label);
		button.RegisterPropertyChangedCallback(IsEnabledProperty, (_, _) => icon.Source = button.IsEnabled ? enabled : disabled);
		return button;
	}
	private async Task Edit()
	{
		if (!_ready || Selected is not { } selected)
		{
			return;
		}

		try
		{ Refresh(await _session.EditTransaction(_owner, selected.Id, null, null)); }
		catch (Exception e) { _error.Text = e.Message; }
	}
	public void Reload()
	{
		try
		{
			TransactionListInfo result = _accountId is Guid account ? _session.Host.Call<ITransactionService, TransactionListInfo>(s => s.GetTransactionsByAccount(account, Day)) : _session.Host.Call<ITransactionService, TransactionListInfo>(s => s.GetTransactions(Day));
			_transactions = result.Transactions;
			_notice.Text = result.LimitExceeded ? "Showing the newest 300 transactions. Choose an earlier date to view older transactions." : "";
			Render(null);
		}
		catch (Exception e) { Fail(e); }
	}
	public void Refresh(Guid? saved)
	{
		try
		{
			TransactionRefreshParam param = new() { Date = Day, AccountId = _accountId };
			param.TransactionIds.AddRange(_transactions.Select(t => t.Id));
			if (saved.HasValue && !param.TransactionIds.Contains(saved.Value) && param.TransactionIds.Count < 300)
			{
				param.TransactionIds.Add(saved.Value);
			}

			_transactions = _session.Host.Call<ITransactionService, TransactionRefreshInfo>(s => s.RefreshTransactions(param)).Transactions;
			Render(saved);
			if (saved.HasValue && !_transactions.Any(t => t.Id == saved.Value))
			{
				_error.Text = "Saved transaction is outside the displayed selection. Navigate or retry to load the newest matching transactions.";
			}
		}
		catch (Exception e) { Fail(e); }
	}
	private void Fail(Exception e) { _ready = false; _error.Text = "Refresh failed. " + e.Message; _list.IsEnabled = false; Actions(); }
	private void Render(Guid? selected)
	{
		selected ??= Selected?.Id;
		string code = _session.CurrencyCode(_session.Settings.BaseCurrencyId);
		if (_accountId is Guid account)
		{
			AccountInfo info = _session.Host.Call<IAccountService, AccountInfo>(s => s.GetById(account));
			_account.Text = info.Name;
			ToolTipService.SetToolTip(_account, info.Name);
			code = _session.CurrencyCode(info.CurrencyId);
		}
		else
		{
			_account.Text = "All accounts";
			ToolTipService.SetToolTip(_account, "All accounts");
		}

		double[] widths = ColumnWidths(_accountId.HasValue);
		for (int i = 0; i < widths.Length; i++)
		{
			_header.ColumnDefinitions[i].Width = new GridLength(widths[i]);
		}
		_header.Children[4].Visibility = _accountId.HasValue ? Visibility.Visible : Visibility.Collapsed;
		List<Grid> rows = new();
		foreach (TransactionInfo transaction in _transactions)
		{
			decimal amount;
			string cumulative = "";
			if (_accountId.HasValue)
			{
				List<TransactionEntryInfo> matching = transaction.Entries.Where(e => e.AccountId == _accountId).ToList();
				amount = matching.Sum(e => e.Amount);
				cumulative = matching.LastOrDefault()?.CumulativeAmount.ToString("N" + _session.Settings.AmountPrecision, System.Globalization.CultureInfo.CurrentCulture) ?? "";
			}
			else
			{
				bool positive = transaction.Entries.Any(e => e.AccountId == _session.Settings.BalancingAccountId && _session.BaseAmount(e.Amount, e.Rate) > 0);
				bool negative = transaction.Entries.Any(e => e.AccountId == _session.Settings.BalancingAccountId && _session.BaseAmount(e.Amount, e.Rate) < 0);
				IEnumerable<decimal> amounts = transaction.Entries.Select(e => _session.BaseAmount(e.Amount, e.Rate));
				amount = positive && !negative ? -amounts.Where(a => a < 0).Sum() : amounts.Where(a => a > 0).Sum();
			}
			Grid row = Row(new[] { transaction.DateTime.ToLocalTime().ToString("d", System.Globalization.CultureInfo.CurrentCulture) + " " + transaction.DateTime.ToLocalTime().ToString("HH:mm", System.Globalization.CultureInfo.CurrentCulture), transaction.Description ?? "", transaction.State.ToString(), amount.ToString("N" + _session.Settings.AmountPrecision, System.Globalization.CultureInfo.CurrentCulture), cumulative, code }, _accountId.HasValue);
			row.Tag = transaction;
			rows.Add(row);
		}
		_list.ItemsSource = rows;
		_list.SelectedItem = rows.FirstOrDefault(r => ((TransactionInfo)r.Tag).Id == selected);
		_ready = true;
		_list.IsEnabled = true;
		_error.Text = "";
		Actions();
	}
	private static double[] ColumnWidths(bool cumulative)
	{
		Dictionary<string, double> columns = ((App)Application.Current).UiSettings.TransactionGrid.Columns;
		return [columns["dateTime"], columns["description"] + (cumulative ? 0 : columns["cumulativeAmount"]),
			columns["state"], columns["amount"], cumulative ? columns["cumulativeAmount"] : 0, columns["currency"]];
	}

	private static Grid Row(string[] values, bool cumulative, bool header = false)
	{
		double[] widths = ColumnWidths(cumulative);
		Grid grid = new() { Width = widths.Sum(), Height = Ui.RowHeight, HorizontalAlignment = HorizontalAlignment.Left };
		foreach (double width in widths)
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width) });
		}

		for (int i = 0; i < values.Length; i++)
		{
			TextBlock text = new() { Text = values[i], VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, Margin = new Thickness(8, 0, 8, 0), Visibility = i == 4 && !cumulative ? Visibility.Collapsed : Visibility.Visible, TextAlignment = header || i == 2 ? TextAlignment.Center : i is 3 or 4 ? TextAlignment.Right : TextAlignment.Left };
			ToolTipService.SetToolTip(text, values[i]);
			Grid.SetColumn(text, i);
			grid.Children.Add(text);
		}
		return grid;
	}
}
