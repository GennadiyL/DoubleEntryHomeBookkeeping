using Business.Models.Constants;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using Dehb.WinUi.Catalogs;
using Dehb.WinUi.Common;
using Dehb.WinUi.Dialogs;
using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Templates;
using Business.Contracts.Services.Transactions;
using Business.Contracts.Services.Trees;
using Business.Models.Enums;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Media.Imaging;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;

namespace Dehb.WinUi.Hosting;

internal sealed partial class UiSession
{
	public async Task<Guid?> EditTransaction(Window owner, Guid? id, Guid? filter, TransactionParam? prepared)
	{
		TransactionInfo? existing = id.HasValue ? Host.Call<ITransactionService, TransactionInfo>(s => s.GetById(id.Value)) : null;
		DateTime original = existing?.DateTime.ToLocalTime() ?? prepared?.DateTime.ToLocalTime() ?? DateTime.Now;
		UiSettingsTypes.UiTransactionEditorSettings popup = ((App)Application.Current).UiSettings.TransactionEditor;
		using EditorWindow window = new(owner, Coordinator, "Transaction.Editor", id.HasValue ? "Edit transaction" : "New transaction", new Windows.Graphics.SizeInt32(popup.Width, popup.Height));
		CalendarDatePicker date = new() { Width = popup.DateWidth, MinWidth = 0, Date = new DateTimeOffset(original.Date) };
		TimePicker time = new() { Width = popup.TimeWidth, MinWidth = 0, ClockIdentifier = "24HourClock", Time = new TimeSpan(original.Hour, original.Minute, 0) };
		time.Loaded += (_, _) =>
		{
			if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(time) > 0 &&
				Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(time, 0) is FrameworkElement templateRoot &&
				templateRoot.FindName("FlyoutButton") is Button flyoutButton)
			{
				flyoutButton.MinWidth = 0;
			}
		};
		TransactionState selectedState = existing?.State ?? prepared?.State ?? TransactionState.Confirmed;
		StackPanel stateButtons = Ui.Row();
		Microsoft.UI.Xaml.Media.SolidColorBrush selectedStateBackground = new(Ui.ParseColor(((App)Application.Current).UiSettings.MainWindow.SideBarBackColor));
		foreach (TransactionState state in new[] { TransactionState.Draft, TransactionState.Planned, TransactionState.Confirmed })
		{
			string label = state.ToString();
			ToggleButton button = new()
			{
				Width = 40, Height = 40, Padding = new Thickness(6),
				IsChecked = selectedState == state,
				Content = new Image
				{
					Width = 24, Height = 24,
					Source = new SvgImageSource(new Uri($"ms-appx:///Assets/Icons/{label.ToLowerInvariant()}-state-enabled.svg"))
				},
				MinWidth = 0,
				VerticalAlignment = VerticalAlignment.Center
			};
			button.Resources["ToggleButtonBackgroundChecked"] = selectedStateBackground;
			button.Resources["ToggleButtonBackgroundCheckedPointerOver"] = selectedStateBackground;
			button.Resources["ToggleButtonBackgroundCheckedPressed"] = selectedStateBackground;
			ToolTipService.SetToolTip(button, label);
			AutomationProperties.SetName(button, label);
			button.Checked += (_, _) =>
			{
				selectedState = state;
				foreach (ToggleButton other in stateButtons.Children.OfType<ToggleButton>())
				{
					if (other != button)
					{
						other.IsChecked = false;
					}
				}
			};
			button.Unchecked += (_, _) =>
			{
				if (selectedState == state)
				{
					button.IsChecked = true;
				}
			};
			stateButtons.Children.Add(button);
			(VirtualKey key, string keyText) = state switch
			{
				TransactionState.Draft => (VirtualKey.Number1, "1"),
				TransactionState.Planned => (VirtualKey.Number2, "2"),
				_ => (VirtualKey.Number3, "3")
			};
			window.AddShortcut(button, key, VirtualKeyModifiers.Menu, $"{label} (Alt+{keyText})",
				() => button.IsChecked = true);
		}
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
		void FocusEntry(EntryLine? entry)
		{
			entries.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (!description.IsLoaded || !ReferenceEquals(Coordinator.Top(window), window)) { return; }
				if (entry is not null && lines.Contains(entry))
				{
					entries.UpdateLayout();
					ViewShortcuts.FocusFirst(entry.View);
					entry.View.StartBringIntoView();
				}
				else if (lines.Count == 0)
				{
					description.Focus(FocusState.Keyboard);
				}
			});
		}
		EntryLine Add()
		{
			EntryLine line = new(this, window, false, Day, Changed, removed =>
			{
				int index = lines.IndexOf(removed);
				if (index < 0) { return; }
				lines.RemoveAt(index);
				entries.Children.Remove(removed.View);
				Changed();
				FocusEntry(lines.Count > 0 ? lines[Math.Max(0, index - 1)] : null);
			});
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

		Grid body = new() { RowSpacing = 12 };
		body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		body.RowDefinitions.Add(new RowDefinition());
		body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		StackPanel fields = new() { Spacing = 12 };
		description.Header = null;
		fields.Children.Add(Ui.Row(Ui.EditorField("Date", date), Ui.EditorField("Time", time), Ui.EditorField("State", stateButtons)));
		fields.Children.Add(Ui.EditorField("Description", description));
		body.Children.Add(fields);
		Grid header = new() { ColumnSpacing = 8, Margin = new Thickness(0, 0, 20, 0) };
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(popup.AmountWidth) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(popup.CurrencyWidth) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(popup.RateWidth + 40) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(popup.BaseAmountWidth) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
		string[] labels = ["Account", "Amount", "Currency", "Rate", "Base amount"];
		for (int i = 0; i < labels.Length; i++)
		{
			TextBlock label = Ui.Text(labels[i]);
			label.TextAlignment = TextAlignment.Center;
			Grid.SetColumn(label, i);
			header.Children.Add(label);
		}
		Grid.SetRow(header, 1);
		body.Children.Add(header);
		ScrollViewer entryScroll = new()
		{
			Content = entries,
			Style = (Style)Application.Current.Resources["PersistentListScrollViewerStyle"],
			VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalContentAlignment = HorizontalAlignment.Stretch
		};
		entryScroll.LayoutUpdated += (_, _) =>
		{
			if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(entryScroll) > 0 &&
				Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(entryScroll, 0) is Grid scrollLayout &&
				scrollLayout.Children.OfType<Microsoft.UI.Xaml.Controls.Primitives.ScrollBar>().FirstOrDefault() is { } bar)
			{
				bar.IsEnabled = entryScroll.ScrollableHeight > 0;
			}
		};
		Grid.SetRow(entryScroll, 2);
		body.Children.Add(entryScroll);
		Grid.SetRow(difference, 3);
		body.Children.Add(difference);
		Grid.SetRow(warning, 4);
		body.Children.Add(warning);
		window.SetFieldContent(body);
		ViewShortcuts.Attach(body, () => ViewShortcuts.FocusFirst(entries));
		Button balanceEntry = Ui.DialogButton("Add balancing entry", () =>
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

			Guid accountId = EntitiesIds.BalancingAccountId;
			AccountInfo account = Host.Call<IAccountService, AccountInfo>(s => s.GetById(accountId));
			if (account.CurrencyId != Settings.BaseCurrencyId)
			{
				throw new InvalidOperationException("Balancing account must use the base currency.");
			}

			EntryLine added = Add();
			added.SetAccount(accountId, false);
			added.Rate.Value = 1m;
			added.Amount.Value = -total;
			FocusEntry(added);
		}, window.Error);
		Button addEntry = Ui.DialogButton("Add entry", () => FocusEntry(Add()), window.Error);
		window.AddShortcut(addEntry, VirtualKey.Insert, VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift,
			"Add entry (Alt+Shift+Insert)", () => new ButtonAutomationPeer(addEntry).Invoke());
		window.AddShortcut(balanceEntry, VirtualKey.B, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
			"Add balancing entry (Ctrl+Shift+B)", () => new ButtonAutomationPeer(balanceEntry).Invoke());
		foreach (Button button in new[] { balanceEntry, addEntry })
		{
			button.Width = double.NaN;
			button.Padding = new Thickness(12, 0, 12, 0);
			window.RightCommands.Children.Add(button);
		}
		string Snapshot() => $"{date.Date}|{time.Time}|{selectedState}|{description.Text}|" + string.Join(";", lines.Select(l => l.Snapshot()));
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

			TransactionParam param = new() { DateTime = local.ToUniversalTime(), Description = description.Text, State = selectedState };
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
		UiSettingsTypes.UiTemplateEditorSettings popup = ((App)Application.Current).UiSettings.TemplateEditor;
		using EditorWindow window = new(owner, Coordinator, "Templates.ElementEditor", "Template", new Windows.Graphics.SizeInt32(popup.Width, popup.Height));
		TextBox name = new() { Header = "Name", Text = old?.Name ?? "" };
		ReferenceField group = new(this, window, "Templates", true, false, old?.GroupId ?? groupId, false, true);
		group.AddShortcuts(window, VirtualKey.G, "G", "Group");
		TextBox description = new() { Header = "Description", Text = old?.Description ?? prepared?.Description ?? "" };
		CheckBox favorite = new() { Content = "Favorite", IsChecked = old?.IsFavorite ?? false };
		window.AddShortcut(favorite, VirtualKey.S, VirtualKeyModifiers.Control, "Favorite (Ctrl+S)",
			() => favorite.IsChecked = favorite.IsChecked != true);
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
			EntryLine line = new(this, window, true, () => DateOnly.FromDateTime(DateTime.Today), Changed, removed =>
			{
				int index = lines.IndexOf(removed);
				if (index < 0) { return; }
				lines.RemoveAt(index);
				entries.Children.Remove(removed.View);
				Changed();
				EntryLine? previous = lines.Count > 0 ? lines[Math.Max(0, index - 1)] : null;
				entries.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
				{
					if (!name.IsLoaded || !ReferenceEquals(Coordinator.Top(window), window)) { return; }
					if (previous is not null && lines.Contains(previous))
					{
						entries.UpdateLayout();
						ViewShortcuts.FocusFirst(previous.View);
						previous.View.StartBringIntoView();
					}
					else if (lines.Count == 0)
					{
						name.Focus(FocusState.Keyboard);
					}
				});
			});
			lines.Add(line);
			entries.Children.Add(line.View);
			Changed();
			return line;
		}
		foreach (TemplateEntryInfo entry in old?.Entries ?? prepared?.Entries ?? new())
		{ EntryLine line = Add(); line.SetAccount(entry.AccountId, false); line.Amount.Value = entry.Amount; }
		name.Header = null;
		description.Header = null;
		StackPanel fields = new() { Spacing = 12 };
		fields.Children.Add(Ui.EditorField("Name", name));
		fields.Children.Add(Ui.EditorField("Group", group));
		fields.Children.Add(Ui.EditorField("Description", description));
		fields.Children.Add(favorite);
		Grid body = new() { RowSpacing = 12 };
		body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		body.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		body.RowDefinitions.Add(new RowDefinition());
		body.Children.Add(fields);
		Grid header = new() { ColumnSpacing = 8, Margin = new Thickness(0, 0, 20, 0) };
		header.ColumnDefinitions.Add(new ColumnDefinition());
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(popup.AmountWidth) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(popup.CurrencyWidth) });
		header.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(32) });
		string[] labels = ["Account", "Amount", "Currency"];
		for (int i = 0; i < labels.Length; i++)
		{
			TextBlock label = Ui.Text(labels[i]);
			label.TextAlignment = TextAlignment.Center;
			Grid.SetColumn(label, i);
			header.Children.Add(label);
		}
		Grid.SetRow(header, 1);
		body.Children.Add(header);
		ScrollViewer entryScroll = new()
		{
			Content = entries,
			Style = (Style)Application.Current.Resources["PersistentListScrollViewerStyle"],
			VerticalScrollBarVisibility = ScrollBarVisibility.Visible,
			HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
			HorizontalContentAlignment = HorizontalAlignment.Stretch
		};
		entryScroll.LayoutUpdated += (_, _) =>
		{
			if (Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChildrenCount(entryScroll) > 0 &&
				Microsoft.UI.Xaml.Media.VisualTreeHelper.GetChild(entryScroll, 0) is Grid scrollLayout &&
				scrollLayout.Children.OfType<Microsoft.UI.Xaml.Controls.Primitives.ScrollBar>().FirstOrDefault() is { } bar)
			{
				bar.IsEnabled = entryScroll.ScrollableHeight > 0;
			}
		};
		Grid.SetRow(entryScroll, 2);
		body.Children.Add(entryScroll);
		window.SetFieldContent(body);
		ViewShortcuts.Attach(body, () => ViewShortcuts.FocusFirst(entries));
		Button addEntry = Ui.DialogButton("Add entry", () =>
		{
			EntryLine added = Add();
			entries.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (!name.IsLoaded || !lines.Contains(added) || !ReferenceEquals(Coordinator.Top(window), window)) { return; }
				entries.UpdateLayout();
				ViewShortcuts.FocusFirst(added.View);
				added.View.StartBringIntoView();
			});
		}, window.Error);
		addEntry.Width = double.NaN;
		addEntry.Padding = new Thickness(12, 0, 12, 0);
		window.RightCommands.Children.Add(addEntry);
		window.AddShortcut(addEntry, VirtualKey.Insert, VirtualKeyModifiers.Menu | VirtualKeyModifiers.Shift,
			"Add entry (Alt+Shift+Insert)", () => new ButtonAutomationPeer(addEntry).Invoke());
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
