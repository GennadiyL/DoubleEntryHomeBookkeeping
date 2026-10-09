using Business.Contracts.Services;
using Business.Contracts.Services.Accounts;
using Business.Contracts.Services.Currencies;
using Business.Contracts.Services.Trees;
using Business.Models.Entities.Config;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace Dehb.WinUi;

internal sealed partial class UiSession : IDisposable
{
	public BusinessHost Host { get; } = new();
	public PopupCoordinator Coordinator { get; } = new();
	public SystemConfig Settings { get; }
	public UiSession()
	{
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
		EditorWindow window = new(owner, Coordinator, key, "Select " + catalog);
		CatalogView view = new(this, window, catalog, groups, readOnly, true) { Height = 540 };
		window.Fields.Children.Add(view);
		Guid? result = null;
		Button accept = Ui.Button("OK", () =>
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
		window.Commands.Children.Add(Ui.Button("Cancel", window.Close, window.Error));
		view.Reload(current, false);
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
		EditorWindow window = new(owner, Coordinator, catalog + (group ? ".GroupEditor" : ".ElementEditor"), (id.HasValue ? "Edit " : "Add ") + (group ? "group" : catalog));
		TextBox name = new() { Header = "Name", Text = oldGroup?.Name ?? oldElement?.Name ?? "" };
		TextBox description = new() { Header = "Description", Text = oldGroup?.Description ?? oldElement?.Description ?? "" };
		CheckBox favorite = new() { Content = "Favorite", IsChecked = oldGroup?.IsFavorite ?? oldElement?.IsFavorite ?? false };
		ReferenceField destination = new(this, window, catalog, true, false, oldGroup?.ParentId ?? oldElement?.GroupId ?? parent ?? tree.Groups.Single(g => g.IsRoot).Id, group);
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

		window.Fields.Children.Add(name);
		window.Fields.Children.Add(Ui.Text(group ? "Parent" : "Group"));
		window.Fields.Children.Add(destination);
		window.Fields.Children.Add(description);
		window.Fields.Children.Add(favorite);
		ReferenceField? correspondent = null, category = null, project = null;
		ComboBox? currency = null;
		if (!group && catalog == "Accounts")
		{
			currency = new() { Header = "Currency", ItemsSource = Currencies(), DisplayMemberPath = "Code", SelectedValuePath = "Id", SelectedValue = account?.CurrencyId, IsEnabled = !id.HasValue };
			correspondent = new(this, window, "Correspondents", false, true, account?.CorrespondentId, false);
			category = new(this, window, "Categories", false, true, account?.CategoryId, false);
			project = new(this, window, "Projects", false, true, account?.ProjectId, false);
			window.Fields.Children.Add(currency);
			window.Fields.Children.Add(Ui.Text("Correspondent"));
			window.Fields.Children.Add(correspondent);
			window.Fields.Children.Add(Ui.Text("Category"));
			window.Fields.Children.Add(category);
			window.Fields.Children.Add(Ui.Text("Project"));
			window.Fields.Children.Add(project);
			window.Fields.Children.Add(Ui.Button("Restore name", () => name.Text = Host.Call<IAccountService, string>(s => s.GetDefaultName(correspondent.Id, category.Id, project.Id, currency.SelectedValue as Guid?)), window.Error));
		}
		string Snapshot() => System.Text.Json.JsonSerializer.Serialize(new
		{
			name.Text,
			Description = description.Text,
			Favorite = favorite.IsChecked,
			destination.Id,
			Currency = currency?.SelectedValue,
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
				result = source.SaveGroup(id, new GroupParam { Name = name.Text, ParentId = groupId, Description = description.Text, IsFavorite = favorite.IsChecked == true });
			}
			else if (catalog == "Accounts")
			{
				Guid currencyId = currency?.SelectedValue as Guid? ?? throw new InvalidOperationException("Choose a currency.");
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
				{ Host.Call<IAccountService>(s => s.Update(id.Value, param)); result = id; }
				else
				{
					result = Host.Call<IAccountService, Guid>(s => s.Add(param));
				}
			}
			else
			{
				result = source.SaveElement(id, new ElementParam { Name = name.Text, GroupId = groupId, Description = description.Text, IsFavorite = favorite.IsChecked == true });
			}
		});
		await window.Show();
		return result;
	}
	public void Dispose() => Host.Dispose();
}
