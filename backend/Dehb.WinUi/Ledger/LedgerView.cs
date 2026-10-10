using Dehb.WinUi.Common;
using Dehb.WinUi.Hosting;
using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Templates;
using Business.Contracts.Services.Transactions;
using Business.Models.Enums;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dehb.WinUi.Ledger;

internal sealed partial class LedgerView : UserControl
{
	private static readonly string[] Headers = ["Date/time", "Description", "Amount", "", "Currency"];
	private readonly UiSession _session;
	private readonly Window _owner;
	private readonly CalendarDatePicker _date = new() { Date = DateTimeOffset.Now };
	private readonly ComboBox _step = new() { ItemsSource = new[] { "Week", "Month", "Quarter", "Year" }, SelectedIndex = 1 };
	private readonly ListView _list = new() { SelectionMode = ListViewSelectionMode.Single };
	private readonly TextBlock _error = Ui.Text("");
	private readonly TextBlock _notice = Ui.Text("");
	private readonly Button _account;
	private readonly StackPanel _actions = Ui.Row();
	private readonly Grid _header;
	private List<TransactionInfo> _transactions = new();
	private Guid? _accountId;
	private bool _ready;
	private DateOnly Day => DateOnly.FromDateTime(_date.Date?.DateTime ?? DateTime.Today);
	private TransactionInfo? Selected => (_list.SelectedItem as Grid)?.Tag as TransactionInfo;
	public LedgerView(UiSession session, Window owner)
	{
		_session = session;
		_owner = owner;
		Ui.StretchRows(_list);
		_account = Ui.AsyncButton("All accounts", async () =>
		{
			Guid? chosen = await session.Pick(owner, "Accounts", false, _accountId, _ => true);
			if (chosen.HasValue)
			{ _accountId = chosen; Reload(); }
			else
			{
				Refresh(null);
			}
		}, _error);
		Grid layout = new() { RowSpacing = 10 };
		for (int i = 0; i < 5; i++)
		{
			layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		}

		layout.RowDefinitions.Add(new RowDefinition());
		layout.Children.Add(Ui.Row(_account, Ui.Button("Clear", () => { _accountId = null; Reload(); }, _error), _date,
			Ui.Button("Today", () => _date.Date = DateTimeOffset.Now, _error), Ui.Button("Previous", () => Navigate(-1), _error), Ui.Button("Next", () => Navigate(1), _error), _step));
		Grid.SetRow(_actions, 1);
		layout.Children.Add(_actions);
		Grid.SetRow(_notice, 2);
		layout.Children.Add(_notice);
		Grid.SetRow(_error, 3);
		layout.Children.Add(_error);
		_header = Row(Headers, false);
		Grid.SetRow(_header, 4);
		layout.Children.Add(_header);
		Grid.SetRow(_list, 5);
		layout.Children.Add(_list);
		Content = layout;
		_list.DoubleTapped += async (_, _) => await Edit();
		_list.SelectionChanged += (_, _) => Actions();
		_date.DateChanged += (_, _) => Reload();
		Actions();
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
		_actions.Children.Add(delete);
		if (!_ready)
		{
			foreach (Button button in _actions.Children.OfType<Button>().Skip(1))
			{
				button.IsEnabled = false;
			}
		}
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
			_account.Content = info.Name;
			code = _session.CurrencyCode(info.CurrencyId);
		}
		else
		{
			_account.Content = "All accounts";
		}

		((TextBlock)_header.Children[3]).Text = _accountId.HasValue ? "Cumulative amount" : "";
		_header.ColumnDefinitions[3].Width = new GridLength(_accountId.HasValue ? 1.5 : 0, GridUnitType.Star);
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
			Grid row = Row(new[] { transaction.DateTime.ToLocalTime().ToString("d", System.Globalization.CultureInfo.CurrentCulture) + " " + transaction.DateTime.ToLocalTime().ToString("HH:mm", System.Globalization.CultureInfo.CurrentCulture) + (transaction.State == TransactionState.Draft ? "  Draft" : ""), transaction.Description ?? "", amount.ToString("N" + _session.Settings.AmountPrecision, System.Globalization.CultureInfo.CurrentCulture), cumulative, code }, _accountId.HasValue);
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
	private static Grid Row(string[] values, bool cumulative)
	{
		Grid grid = new() { ColumnSpacing = 12, HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 760 };
		foreach (double width in new[] { 2.0, 3, 1.3, cumulative ? 1.5 : 0, 0.8 })
		{
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(width, GridUnitType.Star) });
		}

		for (int i = 0; i < values.Length; i++)
		{
			TextBlock text = new() { Text = values[i], VerticalAlignment = VerticalAlignment.Center, TextTrimming = TextTrimming.CharacterEllipsis, TextAlignment = i is 2 or 3 ? TextAlignment.Right : TextAlignment.Left };
			ToolTipService.SetToolTip(text, values[i]);
			Grid.SetColumn(text, i);
			grid.Children.Add(text);
		}
		return grid;
	}
}
