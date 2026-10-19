using Dehb.WinUi.Common;
using Dehb.WinUi.Hosting;
using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Transactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUi.Controls.DecimalBoxControl;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;

namespace Dehb.WinUi.Dialogs;

internal sealed class EntryLine
{
	private readonly UiSession _session;
	private readonly EditorWindow _owner;
	private readonly Func<DateOnly> _date;
	private readonly Action _changed;
	private readonly bool _template;
	private readonly Button _account;
	private readonly TextBox? _accountName;
	private readonly TextBlock _currency = Ui.Text("");
	private readonly TextBlock _base = Ui.Text("");
	public Guid? AccountId { get; private set; }
	public Guid? CurrencyId { get; private set; }
	public DecimalBox Amount { get; }
	public DecimalBox Rate { get; }
	public Grid View { get; } = new() { ColumnSpacing = 8 };
	public EntryLine(UiSession session, EditorWindow owner, bool template, Func<DateOnly> date, Action changed, Action<EntryLine> remove)
	{
		_session = session;
		_owner = owner;
		_date = date;
		_changed = changed;
		_template = template;
		Amount = new() { Precision = session.Settings.AmountPrecision, Value = 0m, MinWidth = 140 };
		Rate = new() { Precision = session.Settings.RatePrecision, Value = 1m, MinWidth = 130 };
		_account = Ui.AsyncButton("Select account", async () =>
		{
			Guid? id = await session.Pick(owner, "Accounts", false, AccountId, _ => true);
			if (id.HasValue)
			{
				SetAccount(id.Value, true);
			}
		}, owner.Error);
		if (template)
		{
			UiSettingsTypes.UiTemplateEditorSettings settings = ((App)Application.Current).UiSettings.TemplateEditor;
			View.ColumnDefinitions.Add(new ColumnDefinition());
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.AmountWidth) });
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.CurrencyWidth) });
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
			_accountName = new TextBox { IsReadOnly = true, PlaceholderText = "Select account", MinWidth = 0 };
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_accountName, "Account");
			_account.Content = "...";
			_account.MinWidth = _account.MinHeight = 0;
			_account.Padding = new Thickness(0);
			_account.VerticalAlignment = VerticalAlignment.Top;
			ToolTipService.SetToolTip(_account, "Choose account");
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_account, "Choose account");
			_accountName.SizeChanged += (_, args) => _account.Width = _account.Height = args.NewSize.Height;
			Grid accountPicker = new() { ColumnSpacing = 8 };
			accountPicker.ColumnDefinitions.Add(new ColumnDefinition());
			accountPicker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			accountPicker.Children.Add(_accountName);
			Grid.SetColumn(_account, 1);
			accountPicker.Children.Add(_account);
			View.Children.Add(accountPicker);
			Amount.MinWidth = 0;
			Amount.Width = settings.AmountWidth;
			Amount.VerticalAlignment = VerticalAlignment.Top;
			Grid.SetColumn(Amount, 1);
			View.Children.Add(Amount);
			_currency.VerticalAlignment = VerticalAlignment.Center;
			_currency.TextTrimming = TextTrimming.CharacterEllipsis;
			Grid.SetColumn(_currency, 2);
			View.Children.Add(_currency);
			Button deleteEntry = Ui.Button("", () => remove(this), owner.Error);
			deleteEntry.Content = new FontIcon { Glyph = "\uE74D", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black) };
			deleteEntry.Width = deleteEntry.Height = 32;
			deleteEntry.MinWidth = deleteEntry.MinHeight = 0;
			deleteEntry.Padding = new Thickness(0);
			deleteEntry.VerticalAlignment = VerticalAlignment.Top;
			ToolTipService.SetToolTip(deleteEntry, "Remove entry");
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(deleteEntry, "Remove entry");
			owner.AddShortcut(_account, VirtualKey.Number9, VirtualKeyModifiers.Control, "Choose account (Ctrl+9)",
				() => new ButtonAutomationPeer(_account).Invoke(), View);
			owner.AddShortcut(deleteEntry, VirtualKey.Delete, VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift, "Remove entry (Alt+Shift+Delete)",
				() => new ButtonAutomationPeer(deleteEntry).Invoke(), View);
			Grid.SetColumn(deleteEntry, 3);
			View.Children.Add(deleteEntry);
		}
		else
		{
			UiSettingsTypes.UiTransactionEditorSettings settings = ((App)Application.Current).UiSettings.TransactionEditor;
			View.ColumnDefinitions.Add(new ColumnDefinition());
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.AmountWidth) });
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.CurrencyWidth) });
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.RateWidth + 40) });
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.BaseAmountWidth) });
			View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
			_accountName = new TextBox { IsReadOnly = true, PlaceholderText = "Select account", MinWidth = 0 };
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_accountName, "Account");
			_account.Content = "...";
			_account.MinWidth = _account.MinHeight = 0;
			_account.Padding = new Thickness(0);
			_account.VerticalAlignment = VerticalAlignment.Top;
			ToolTipService.SetToolTip(_account, "Choose account");
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(_account, "Choose account");
			_accountName.SizeChanged += (_, args) => _account.Width = _account.Height = args.NewSize.Height;
			Grid accountPicker = new() { ColumnSpacing = 8 };
			accountPicker.ColumnDefinitions.Add(new ColumnDefinition());
			accountPicker.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
			accountPicker.Children.Add(_accountName);
			Grid.SetColumn(_account, 1);
			accountPicker.Children.Add(_account);
			View.Children.Add(accountPicker);
			Amount.MinWidth = 0;
			Amount.Width = settings.AmountWidth;
			Amount.VerticalAlignment = VerticalAlignment.Top;
			Grid.SetColumn(Amount, 1);
			View.Children.Add(Amount);
			_currency.VerticalAlignment = VerticalAlignment.Center;
			_currency.TextTrimming = TextTrimming.CharacterEllipsis;
			Grid.SetColumn(_currency, 2);
			View.Children.Add(_currency);
			Button deleteEntry = Ui.Button("", () => remove(this), owner.Error);
			deleteEntry.Content = new FontIcon { Glyph = "\uE74D", Foreground = new Microsoft.UI.Xaml.Media.SolidColorBrush(Microsoft.UI.Colors.Black) };
			deleteEntry.Width = deleteEntry.Height = 32;
			deleteEntry.MinWidth = deleteEntry.MinHeight = 0;
			deleteEntry.Padding = new Thickness(0);
			deleteEntry.VerticalAlignment = VerticalAlignment.Top;
			ToolTipService.SetToolTip(deleteEntry, "Remove entry");
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(deleteEntry, "Remove entry");
			owner.AddShortcut(_account, VirtualKey.Number9, VirtualKeyModifiers.Control, "Choose account (Ctrl+9)",
				() => new ButtonAutomationPeer(_account).Invoke(), View);
			owner.AddShortcut(deleteEntry, VirtualKey.Delete, VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift, "Remove entry (Alt+Shift+Delete)",
				() => new ButtonAutomationPeer(deleteEntry).Invoke(), View);
			Grid.SetColumn(deleteEntry, 5);
			View.Children.Add(deleteEntry);
			Rate.MinWidth = 0;
			Rate.Width = settings.RateWidth;
			Rate.VerticalAlignment = VerticalAlignment.Top;
			Button restore = Ui.Button("\u21BB", RestoreRate, owner.Error);
			restore.Width = restore.Height = 32;
			restore.MinWidth = restore.MinHeight = 0;
			restore.Padding = new Thickness(0);
			restore.VerticalAlignment = VerticalAlignment.Top;
			ToolTipService.SetToolTip(restore, "Restore rate");
			Microsoft.UI.Xaml.Automation.AutomationProperties.SetName(restore, "Restore rate");
			owner.AddShortcut(restore, VirtualKey.R, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift, "Restore rate (Ctrl+Shift+R)",
				() => new ButtonAutomationPeer(restore).Invoke(), View);
			Grid rateCell = new() { ColumnSpacing = 8 };
			rateCell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(settings.RateWidth) });
			rateCell.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
			rateCell.Children.Add(Rate);
			Grid.SetColumn(restore, 1);
			rateCell.Children.Add(restore);
			Grid.SetColumn(rateCell, 3);
			View.Children.Add(rateCell);
			_base.VerticalAlignment = VerticalAlignment.Center;
			_base.TextAlignment = TextAlignment.Right;
			_base.TextTrimming = TextTrimming.CharacterEllipsis;
			Grid.SetColumn(_base, 4);
			View.Children.Add(_base);
		}
		Amount.PropertyChanged += (_, e) => { if (e.PropertyName == "Value")
			{
				Update();
			}
		};
		Rate.PropertyChanged += (_, e) => { if (e.PropertyName == "Value")
			{
				Update();
			}
		};
	}
	public void SetAccount(Guid id, bool restore)
	{
		AccountInfo account = _session.Host.Call<IAccountService, AccountInfo>(s => s.GetById(id));
		decimal? rate = restore && CurrencyId != account.CurrencyId && !_template ? _session.Rate(id, _date()) : null;
		AccountId = account.Id;
		CurrencyId = account.CurrencyId;
		if (_accountName is not null)
		{
			_accountName.Text = account.Name;
		}
		else
		{
			_account.Content = account.Name;
		}
		_currency.Text = _session.CurrencyCode(account.CurrencyId);
		Rate.IsReadOnly = CurrencyId == _session.Settings.BaseCurrencyId;
		if (rate.HasValue)
		{
			Rate.Value = rate;
		}

		Update();
	}
	public void RestoreRate()
	{
		if (AccountId is null)
		{
			throw new InvalidOperationException("Choose an account first.");
		}

		Rate.Value = _session.Rate(AccountId.Value, _date());
		Update();
	}
	public decimal BaseAmount => _session.BaseAmount(Amount.Value ?? 0m, Rate.Value ?? 0m);
	public TransactionEntryParam Read() => new() { AccountId = AccountId ?? throw new InvalidOperationException("Every entry requires an account."), Amount = Ui.Number(Amount), Rate = _template ? 1m : Ui.Number(Rate) };
	public string Snapshot() => $"{AccountId}|{Amount.Text}|{Rate.Text}";
	private void Update()
	{
		try
		{ _base.Text = BaseAmount.ToString("N" + _session.Settings.AmountPrecision, System.Globalization.CultureInfo.CurrentCulture); _changed(); }
		catch (Exception e) { _owner.Error.Text = e.Message; }
	}
}
