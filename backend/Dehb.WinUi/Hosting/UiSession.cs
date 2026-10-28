using Dehb.WinUi.Catalogs;
using Dehb.WinUi.Common;
using Dehb.WinUi.Dialogs;
using Dehb.WinUi.UiSettingsTypes;
using Dehb.WinUi.WindowManagement;
using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Currencies;
using Business.Contracts.Services.Trees;
using Business.Models.Entities.Config;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Windows.System;
using Microsoft.UI.Xaml.Automation.Peers;
using Shared.Contracts;

namespace Dehb.WinUi.Hosting;

internal sealed partial class UiSession : IDisposable
{
	public BusinessHost Host { get; }
	public PopupCoordinator Coordinator { get; } = new();
	public SystemConfig Settings { get; }
	public UiSession(ISharedContext sharedContext)
	{
		Host = new BusinessHost(sharedContext);
		try { Settings = Host.ReadSettings(); } catch { Host.Dispose(); throw; }
	}
	public List<CurrencyInfo> Currencies() => Host.Call<ICurrencyService, List<CurrencyInfo>>(s => s.GetAllCurrencies());
	public string CurrencyCode(Guid id) => Currencies().Single(c => c.Id == id).Code;
	public decimal BaseAmount(decimal amount, decimal rate) => decimal.Round(checked(amount * rate), Settings.AmountPrecision, MidpointRounding.ToEven);
	public decimal Rate(Guid account, DateOnly date) => Host.Call<ICurrencyRateService, decimal>(s => s.GetRate(account, date));
	public Task<Guid?> Pick(Window owner, string catalog, bool groups, Guid? current, Func<Guid, bool> eligible) => Pick(owner, catalog, groups, current, eligible, false);
	public async Task<Guid?> Pick(Window owner, string catalog, bool groups, Guid? current, Func<Guid, bool> eligible, bool forceReadOnly)
	{
		bool readOnly = groups && (forceReadOnly || Coordinator.IsActive(catalog + ".Tree") || Coordinator.IsActive(catalog + ".Groups") || Coordinator.IsActive(catalog + ".GroupEditor"));
		string key = catalog + (groups ? readOnly ? ".SelectGroups" : ".Groups" : ".Tree");
		UiSettings settings = ((App)Application.Current).UiSettings;
		UiEditorPopupSettings popup = groups ? settings.GroupTreeGridPopup : settings.ElementTreeGridPopup;
		Windows.Graphics.SizeInt32 fixedSize = new(popup.Width, popup.Height);
		using EditorWindow window = new(owner, Coordinator, key, "Select " + catalog, fixedSize);
		CatalogView view = new(this, window, catalog, groups, readOnly, true);
		window.SetFieldContent(view);
		Guid? result = null;
		Button accept = Ui.DialogButton("OK", () =>
		{
			if (!view.Ready || view.Tree.SelectedNode is not { } node || node.IsGroup != groups || !eligible(CatalogView.Id(node)))
			{
				throw new InvalidOperationException("Select an eligible item.");
			}

			result = CatalogView.Id(node);
			window.Finish(true);
		}, window.Error);
		void Update() => accept.IsEnabled = view.Ready && view.Tree.SelectedNode is { } node && node.IsGroup == groups && eligible(CatalogView.Id(node));
		view.Tree.SelectedNodeChanged += (_, _) => Update();
		view.Accepted += node => { if (eligible(CatalogView.Id(node))) { result = CatalogView.Id(node); window.Finish(true); } };
		window.Commands.Children.Add(accept);
		window.Commands.Children.Add(Ui.DialogButton("Cancel", window.Close, window.Error));
		bool initialized = false;
		view.Loaded += (_, _) => view.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
		{
			if (view.IsLoaded && ReferenceEquals(Coordinator.Top(window), window))
			{
				if (!initialized)
				{
					initialized = true;
					view.Reload(current, false);
					Update();
				}
				view.UpdateLayout();
				view.Tree.FocusRows();
			}
		});
		Update();
		await window.Show();
		return result;
	}
	public async Task<Guid?> EditCatalog(Window owner, string catalog, Guid? id, bool group, Guid? parent)
	{
		if (!group && catalog == "Templates")
		{
			return await EditTemplate(owner, id, parent, null);
		}

		CatalogSource source = CatalogSource.For(Host, catalog);
		TreeInfo tree = source.Read();
		GroupInfo? oldGroup = group && id.HasValue ? tree.Groups.Single(g => g.Id == id) : null;
		if (oldGroup?.IsRoot == true)
		{
			throw new InvalidOperationException("Root cannot be edited.");
		}

		ElementInfo? oldElement = !group && id.HasValue ? tree.Elements.Single(e => e.Id == id) : null;
		AccountInfo? account = !group && catalog == "Accounts" && id.HasValue ? Host.Call<IAccountService, AccountInfo>(s => s.GetById(id.Value)) : null;
		bool fixedEditor = group || catalog is "Accounts" or "Correspondents" or "Categories" or "Projects";
		Windows.Graphics.SizeInt32? fixedSize = null;
		if (fixedEditor)
		{
			UiSettings settings = ((App)Application.Current).UiSettings;
			UiEditorPopupSettings popup = group ? settings.GroupEditor : catalog == "Accounts" ? settings.AccountEditor : settings.ElementEditor;
			fixedSize = new Windows.Graphics.SizeInt32(popup.Width, popup.Height);
		}
		using EditorWindow window = new(owner, Coordinator, catalog + (group ? ".GroupEditor" : ".ElementEditor"), (id.HasValue ? "Edit " : "Add ") + (group ? "group" : catalog), fixedSize);
		TextBox name = new() { Header = "Name", Text = oldGroup?.Name ?? oldElement?.Name ?? "" };
		TextBox description = new() { Header = "Description", Text = oldGroup?.Description ?? oldElement?.Description ?? "" };
		CheckBox favorite = new() { Content = "Favorite", IsChecked = oldGroup?.IsFavorite ?? oldElement?.IsFavorite ?? false };
		ReferenceField destination = new(this, window, catalog, true, false, oldGroup?.ParentId ?? oldElement?.GroupId ?? parent ?? tree.Groups.Single(g => g.IsRoot).Id, group, fixedEditor);
		destination.AddShortcuts(window, VirtualKey.G, "G", group ? "Parent" : "Group");
		window.AddShortcut(favorite, VirtualKey.S, VirtualKeyModifiers.Control, "Favorite (Ctrl+S)",
			() => favorite.IsChecked = favorite.IsChecked != true);
		if (group && id.HasValue)
		{
			destination.Eligible = candidate =>
			{
				TreeInfo current = source.Read();
				GroupInfo? proposed = current.Groups.SingleOrDefault(g => g.Id == candidate);
				while (proposed is not null)
				{
					if (proposed.Id == id)
					{
						return false;
					}

					if (proposed.IsRoot)
					{
						return true;
					}

					proposed = current.Groups.SingleOrDefault(g => g.Id == proposed.ParentId);
				}
				return false;
			};
		}

		if (fixedEditor)
		{
			name.Header = null;
			description.Header = null;
			window.Fields.Children.Add(Ui.EditorField("Name", name));
			window.Fields.Children.Add(Ui.EditorField(group ? "Parent" : "Group", destination));
			window.Fields.Children.Add(Ui.EditorField("Description", description));
		}
		else
		{
			window.Fields.Children.Add(name);
			window.Fields.Children.Add(Ui.Text("Group"));
			window.Fields.Children.Add(destination);
			window.Fields.Children.Add(description);
		}
		window.Fields.Children.Add(favorite);
		ReferenceField? correspondent = null, category = null, project = null;
		ComboBox? currency = null;
		if (!group && catalog == "Accounts")
		{
			FrameworkElement currencyControl;
			if (account is null)
			{
				currency = new() { HorizontalAlignment = HorizontalAlignment.Stretch, MinWidth = 0, ItemsSource = Currencies(), DisplayMemberPath = "Code", SelectedValuePath = "Id" };
				currencyControl = currency;
			}
			else
			{
				currencyControl = new TextBox { Text = CurrencyCode(account.CurrencyId), IsReadOnly = true, MinWidth = 0, HorizontalAlignment = HorizontalAlignment.Stretch };
			}
			correspondent = new(this, window, "Correspondents", false, true, account?.CorrespondentId, false, true);
			category = new(this, window, "Categories", false, true, account?.CategoryId, false, true);
			project = new(this, window, "Projects", false, true, account?.ProjectId, false, true);
			correspondent.AddShortcuts(window, VirtualKey.Number6, "6", "Correspondent");
			category.AddShortcuts(window, VirtualKey.Number7, "7", "Category");
			project.AddShortcuts(window, VirtualKey.Number8, "8", "Project");
			window.Fields.Children.Add(Ui.EditorField("Currency", currencyControl));
			window.Fields.Children.Add(Ui.EditorField("Correspondent", correspondent));
			window.Fields.Children.Add(Ui.EditorField("Category", category));
			window.Fields.Children.Add(Ui.EditorField("Project", project));
			Button restoreName = Ui.DialogButton("Restore name", () => name.Text = Host.Call<IAccountService, string>(s => s.GetDefaultName(correspondent.Id, category.Id, project.Id, account?.CurrencyId ?? (currency?.SelectedValue as Guid?))), window.Error);
			restoreName.Width = double.NaN;
			restoreName.Padding = new Thickness(12, 0, 12, 0);
			window.RightCommands.Children.Add(restoreName);
			window.AddShortcut(restoreName, VirtualKey.R, VirtualKeyModifiers.Control | VirtualKeyModifiers.Shift,
				"Restore name (Ctrl+Shift+R)", () => new ButtonAutomationPeer(restoreName).Invoke());
		}
		string Snapshot() => System.Text.Json.JsonSerializer.Serialize(new
		{
			name.Text,
			Description = description.Text,
			Favorite = favorite.IsChecked,
			destination.Id,
			Currency = account?.CurrencyId ?? (currency?.SelectedValue as Guid?),
			Correspondent = correspondent?.Id,
			Category = category?.Id,
			Project = project?.Id
		});
		string initial = Snapshot();
		window.IsDirty = () => initial != Snapshot();
		Guid? result = null;
		window.SaveButton(() =>
		{
			destination.Refresh();
			correspondent?.Refresh();
			category?.Refresh();
			project?.Refresh();
			Guid groupId = destination.Id ?? throw new InvalidOperationException("Choose a group.");
			if (group)
			{
				GroupParam changes = new() { Name = name.Text, ParentId = groupId, Description = description.Text, IsFavorite = favorite.IsChecked == true };
				if (oldGroup is null)
				{
					result = source.SaveGroup(null, changes);
				}
				else
				{
					if (groupId != oldGroup.ParentId)
					{
						source.Move(true, oldGroup.Id, groupId);
						oldGroup.ParentId = groupId;
						result = oldGroup.Id;
					}
					if (changes.Name != oldGroup.Name || changes.Description != (oldGroup.Description ?? "") ||
						changes.IsFavorite != oldGroup.IsFavorite)
					{
						result = source.SaveGroup(oldGroup.Id, changes);
					}
					result = oldGroup.Id;
				}
			}
			else if (catalog == "Accounts")
			{
				Guid currencyId = account?.CurrencyId ?? (currency?.SelectedValue as Guid?) ?? throw new InvalidOperationException("Choose a currency.");
				if (!id.HasValue && string.IsNullOrWhiteSpace(name.Text))
				{
					name.Text = Host.Call<IAccountService, string>(s => s.GetDefaultName(correspondent!.Id, category!.Id, project!.Id, currencyId));
				}

				AccountParam param = new()
				{
					Name = name.Text,
					GroupId = groupId,
					CurrencyId = currencyId,
					Description = description.Text,
					IsFavorite = favorite.IsChecked == true,
					CorrespondentId = correspondent!.Id,
					CategoryId = category!.Id,
					ProjectId = project!.Id
				};
				if (id.HasValue)
				{
					if (account is not null && groupId != account.GroupId)
					{
						Host.Call<IAccountService>(s => s.MoveToAnotherGroup(id.Value, groupId));
						account.GroupId = groupId;
					}
					if (account is null || param.Name != account.Name || param.Description != (account.Description ?? "") ||
						param.IsFavorite != account.IsFavorite || param.CorrespondentId != account.CorrespondentId ||
						param.CategoryId != account.CategoryId || param.ProjectId != account.ProjectId)
					{
						Host.Call<IAccountService>(s => s.Update(id.Value, param));
					}
					result = id;
				}
				else
				{
					result = Host.Call<IAccountService, Guid>(s => s.Add(param));
				}
			}
			else
			{
				ElementParam changes = new() { Name = name.Text, GroupId = groupId, Description = description.Text, IsFavorite = favorite.IsChecked == true };
				if (oldElement is null)
				{
					result = source.SaveElement(null, changes);
				}
				else
				{
					if (groupId != oldElement.GroupId)
					{
						source.Move(false, oldElement.Id, groupId);
						oldElement.GroupId = groupId;
						result = oldElement.Id;
					}
					if (changes.Name != oldElement.Name || changes.Description != (oldElement.Description ?? "") ||
						changes.IsFavorite != oldElement.IsFavorite)
					{
						result = source.SaveElement(oldElement.Id, changes);
					}
					result = oldElement.Id;
				}
			}
		});
		await window.Show();
		return result;
	}
	public void Dispose() => Host.Dispose();
}
