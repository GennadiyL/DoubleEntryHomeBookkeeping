using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Transactions;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUi.Controls.DecimalBoxControl;

namespace Dehb.WinUi;

internal sealed class EntryLine
{
	private readonly UiSession _session;
	private readonly EditorWindow _owner;
	private readonly Func<DateOnly> _date;
	private readonly Action _changed;
	private readonly bool _template;
	private readonly Button _account;
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
		View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(3, GridUnitType.Star) });
		View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 230 });
		View.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(2, GridUnitType.Star), MinWidth = 230 });
		View.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		View.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		View.Children.Add(_account);
		FrameworkElement amountCell = template ? Amount : Ui.Row(Amount, _currency);
		Grid.SetColumn(amountCell, 1);
		View.Children.Add(amountCell);
		FrameworkElement rateCell = template ? _currency : Ui.Row(Rate, Ui.Button("Restore rate", RestoreRate, owner.Error));
		Grid.SetColumn(rateCell, 2);
		View.Children.Add(rateCell);
		if (!template)
		{ Grid.SetColumn(_base, 3); View.Children.Add(_base); }
		Button delete = Ui.Button("Remove", () => remove(this), owner.Error);
		Grid.SetColumn(delete, 4);
		View.Children.Add(delete);
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
		_account.Content = account.Name;
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
