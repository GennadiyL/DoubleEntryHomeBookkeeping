using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Templates;
using Business.Contracts.Services.Transactions;
using Business.Contracts.Services.Trees;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dehb.WinUi;

internal sealed partial class UiSession
{
	public async Task<Guid?> EditTransaction(Window owner, Guid? id, Guid? filter, TransactionParam? prepared)
	{
		TransactionInfo? existing = id.HasValue ? Host.Call<ITransactionService, TransactionInfo>(s => s.GetById(id.Value)) : null;
		DateTime original = existing?.DateTime.ToLocalTime() ?? prepared?.DateTime.ToLocalTime() ?? DateTime.Now;
		EditorWindow window = new(owner, Coordinator, "Transaction.Editor", id.HasValue ? "Edit transaction" : "New transaction");
		CalendarDatePicker date = new() { Date = new DateTimeOffset(original.Date) };
		TimePicker time = new() { ClockIdentifier = "24HourClock", Time = new TimeSpan(original.Hour, original.Minute, 0) };
		bool timeChanged = false;
		time.TimeChanged += (_, _) => timeChanged = true;
		TextBox description = new() { Header = "Description", Text = existing?.Description ?? prepared?.Description ?? "" };
		StackPanel entries = new() { Spacing = 8 };
		List<EntryLine> lines = new();
		TextBlock difference = Ui.Text("");
		TextBlock warning = Ui.Text("");
		DateOnly Day() => DateOnly.FromDateTime(date.Date?.DateTime ?? throw new InvalidOperationException("Select a date."));
		void Changed()
		{
			decimal sum = lines.Sum(l => l.BaseAmount);
			difference.Text = "Difference: " + sum.ToString("N" + Settings.AmountPrecision, System.Globalization.CultureInfo.CurrentCulture);
			warning.Text = lines.Count < 2 ? "Draft: at least two entries are required." : sum != 0m ? "Draft: entries are not balanced." : "";
			if (id.HasValue)
			{
				Coordinator.Protect(window, lines.Where(l => l.AccountId.HasValue).Select(l => l.AccountId!.Value));
			}
		}
		EntryLine Add()
		{
			EntryLine line = new(this, window, false, Day, Changed, removed => { lines.Remove(removed); entries.Children.Remove(removed.View); Changed(); });
			lines.Add(line);
			entries.Children.Add(line.View);
			Changed();
			return line;
		}
		IEnumerable<TransactionEntryParam> initialEntries = existing?.Entries.Select(e => new TransactionEntryParam { AccountId = e.AccountId, Amount = e.Amount, Rate = e.Rate })
			?? prepared?.Entries ?? Enumerable.Empty<TransactionEntryParam>();
		foreach (TransactionEntryParam value in initialEntries)
		{ EntryLine line = Add(); line.SetAccount(value.AccountId, false); line.Amount.Value = value.Amount; line.Rate.Value = value.Rate; }
		if (existing is null && prepared is null && filter.HasValue)
		{
			Add().SetAccount(filter.Value, true);
		}

		window.Fields.Children.Add(Ui.Row(date, time));
		window.Fields.Children.Add(description);
		window.Fields.Children.Add(Ui.Text("Account                         Amount / Currency                         Rate                         Base amount"));
		window.Fields.Children.Add(entries);
		window.Fields.Children.Add(Ui.Button("Add entry", () => Add(), window.Error));
		window.Fields.Children.Add(difference);
		window.Fields.Children.Add(warning);
		window.Fields.Children.Add(Ui.Button("Add balancing entry", () =>
		{
			foreach (EntryLine line in lines)
			{
				line.Read();
			}

			decimal total = lines.Sum(l => l.BaseAmount);
			if (total == 0m)
			{
				return;
			}

			Guid accountId = Settings.BalancingAccountId ?? throw new InvalidOperationException("No balancing account configured.");
			AccountInfo account = Host.Call<IAccountService, AccountInfo>(s => s.GetById(accountId));
			if (account.CurrencyId != Settings.BaseCurrencyId)
			{
				throw new InvalidOperationException("Balancing account must use the base currency.");
			}

			EntryLine added = Add();
			added.SetAccount(accountId, false);
			added.Rate.Value = 1m;
			added.Amount.Value = -total;
		}, window.Error));
		string Snapshot() => $"{date.Date}|{time.Time}|{description.Text}|" + string.Join(";", lines.Select(l => l.Snapshot()));
		string initial = Snapshot();
		window.IsDirty = () => initial != Snapshot();
		window.BeforeClose = () => Coordinator.Release(window);
		window.Activated += (_, _) =>
		{
			try
			{
				HashSet<Guid> live = Host.Call<IAccountGroupService, AccountTreeInfo>(s => s.GetAccountsTree()).Elements.Select(e => e.Id).ToHashSet();
				foreach (EntryLine line in lines.ToArray())
				{
					if (line.AccountId is not Guid accountId)
					{
						continue;
					}

					if (!id.HasValue && !live.Contains(accountId))
					{ lines.Remove(line); entries.Children.Remove(line.View); }
					else
					{
						line.SetAccount(accountId, false);
					}
				}
				Changed();
			}
			catch (Exception e) { window.Error.Text = e.Message; }
		};
		Guid? saved = null;
		window.SaveButton(() =>
		{
			DateTime local = Day().ToDateTime(TimeOnly.FromTimeSpan(time.Time), DateTimeKind.Local);
			if (existing is not null && !timeChanged)
			{
				local = local.AddTicks(original.TimeOfDay.Ticks % TimeSpan.TicksPerMinute);
			}

			TransactionParam param = new() { DateTime = local.ToUniversalTime(), Description = description.Text };
			param.Entries.AddRange(lines.Select(l => l.Read()));
			if (id.HasValue)
			{ Host.Call<ITransactionService>(s => s.Update(id.Value, param)); saved = id; }
			else
			{
				saved = Host.Call<ITransactionService, Guid>(s => s.Add(param));
			}
		});
		Changed();
		await window.Show();
		return saved;
	}

	public async Task<Guid?> EditTemplate(Window owner, Guid? id, Guid? groupId, FromTransactionInfo? prepared)
	{
		TemplateInfo? old = id.HasValue ? Host.Call<ITemplateService, TemplateInfo>(s => s.GetById(id.Value)) : null;
		EditorWindow window = new(owner, Coordinator, "Templates.ElementEditor", "Template");
		TextBox name = new() { Header = "Name", Text = old?.Name ?? "" };
		ReferenceField group = new(this, window, "Templates", true, false, old?.GroupId ?? groupId, false);
		TextBox description = new() { Header = "Description", Text = old?.Description ?? prepared?.Description ?? "" };
		CheckBox favorite = new() { Content = "Favorite", IsChecked = old?.IsFavorite ?? false };
		StackPanel entries = new() { Spacing = 8 };
		List<EntryLine> lines = new();
		void Changed()
		{ if (id.HasValue)
			{
				Coordinator.Protect(window, lines.Where(l => l.AccountId.HasValue).Select(l => l.AccountId!.Value));
			}
		}
		EntryLine Add()
		{
			EntryLine line = new(this, window, true, () => DateOnly.FromDateTime(DateTime.Today), Changed, removed => { lines.Remove(removed); entries.Children.Remove(removed.View); Changed(); });
			lines.Add(line);
			entries.Children.Add(line.View);
			Changed();
			return line;
		}
		foreach (TemplateEntryInfo entry in old?.Entries ?? prepared?.Entries ?? new())
		{ EntryLine line = Add(); line.SetAccount(entry.AccountId, false); line.Amount.Value = entry.Amount; }
		window.Fields.Children.Add(name);
		window.Fields.Children.Add(Ui.Text("Group"));
		window.Fields.Children.Add(group);
		window.Fields.Children.Add(description);
		window.Fields.Children.Add(favorite);
		window.Fields.Children.Add(Ui.Text("Account                                  Amount                                  Currency"));
		window.Fields.Children.Add(entries);
		window.Fields.Children.Add(Ui.Button("Add entry", () => Add(), window.Error));
		string Snapshot() => $"{name.Text}|{group.Id}|{description.Text}|{favorite.IsChecked}|" + string.Join(";", lines.Select(l => l.Snapshot()));
		string initial = Snapshot();
		window.IsDirty = () => initial != Snapshot();
		window.BeforeClose = () => Coordinator.Release(window);
		window.Activated += (_, _) =>
		{
			try
			{
				HashSet<Guid> live = Host.Call<IAccountGroupService, AccountTreeInfo>(s => s.GetAccountsTree()).Elements.Select(e => e.Id).ToHashSet();
				foreach (EntryLine line in lines.ToArray())
				{
					if (line.AccountId is Guid accountId)
					{
						if (!id.HasValue && !live.Contains(accountId))
						{ lines.Remove(line); entries.Children.Remove(line.View); }
						else
						{
							line.SetAccount(accountId, false);
						}
					}
				}

				Changed();
			}
			catch (Exception e) { window.Error.Text = e.Message; }
		};
		Guid? saved = null;
		window.SaveButton(() =>
		{
			group.Refresh();
			TemplateParam param = new() { Name = name.Text, Description = description.Text, GroupId = group.Id ?? throw new InvalidOperationException("Choose a group."), IsFavorite = favorite.IsChecked == true };
			param.Entries.AddRange(lines.Select(l => { TransactionEntryParam entry = l.Read(); return new TemplateEntryParam { AccountId = entry.AccountId, Amount = entry.Amount }; }));
			if (id.HasValue)
			{ Host.Call<ITemplateService>(s => s.Update(id.Value, param)); saved = id; }
			else
			{
				saved = Host.Call<ITemplateService, Guid>(s => s.Add(param));
			}
		});
		await window.Show();
		return saved;
	}
}
