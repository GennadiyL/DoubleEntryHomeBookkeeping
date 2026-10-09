using Business.Contracts.Services;
using Business.Contracts.Services.Trees;
using Business.Contracts.Services.Accounts;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Collections.ObjectModel;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi;

internal sealed partial class CatalogView : UserControl
{
	private readonly UiSession _session;
	private readonly Window _owner;
	private readonly CatalogSource _source;
	private readonly bool _groupsOnly;
	private readonly bool _readOnly;
	private readonly bool _picker;
	private readonly TextBlock _error = Ui.Text("");
	private readonly StackPanel _commands = Ui.Row();
	private readonly Dictionary<Guid, TreeGridNode> _nodes = new();
	private readonly ColumnWidthSettings _widths = new();
	private TreeGridNode? _combineSource;
	public TreeGrid Tree { get; } = new();
	public bool Ready { get; private set; }
	public event Action<TreeGridNode>? Accepted;

	public CatalogView(UiSession session, Window owner, string catalog, bool groupsOnly, bool readOnly, bool picker)
	{
		_session = session;
		_owner = owner;
		_source = CatalogSource.For(session.Host, catalog);
		_groupsOnly = groupsOnly;
		_readOnly = readOnly;
		_picker = picker;
		Grid layout = new() { RowSpacing = 8, MinHeight = 420 };
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		layout.RowDefinitions.Add(new RowDefinition());
		layout.Children.Add(_commands);
		Grid.SetRow(_error, 1);
		layout.Children.Add(_error);
		Grid.SetRow(Tree, 2);
		layout.Children.Add(Tree);
		Content = layout;
		Tree.NameHeader = catalog;
		Tree.Columns.Add(new TreeGridColumn { Header = "Description", BindingPath = "Data.Description" });
		if (catalog == "Accounts" && !groupsOnly)
		{
			Tree.Columns.Add(new TreeGridColumn { Header = "Currency", BindingPath = "Data.CurrencyName" });
		}

		Tree.AllowDragDrop = !readOnly;
		Tree.AllowMerge = !picker;
		Tree.RowActivated += async (_, node) =>
		{
			if (picker)
			{
				if (Ready && node.IsGroup == groupsOnly)
				{
					Accepted?.Invoke(node);
				}
			}
			else
			{
				await Edit(node);
			}
		};
		Tree.StarChanged += (_, node) =>
		{
			try
			{
				if (readOnly || !Ready)
				{
					throw new InvalidOperationException("Selection is read-only.");
				}

				_source.Favorite(node.IsGroup, Id(node), node.IsStarred);
			}
			catch (Exception e)
			{
				node.IsStarred = !node.IsStarred;
				_error.Text = e.Message;
			}
		};
		Tree.SelectedNodeChanged += (_, _) => Commands();
		Tree.DropRequested += async (_, request) =>
		{
			try
			{
				if (!Ready || readOnly)
				{
					return;
				}

				if (request.Operation == TreeGridDropOperation.Merge)
				{
					if (picker || !await Ui.Confirm(owner, $"Merge '{request.Source.Name}' into '{request.Target.Name}'? Source will be removed."))
					{
						return;
					}

					_source.Merge(request.Source.IsGroup, Id(request.Source), Id(request.Target));
					Reload(Id(request.Target), true);
				}
				else
				{
					if (request.Operation == TreeGridDropOperation.Move)
					{
						_source.Move(request.Source.IsGroup, Id(request.Source), Id(request.DestinationParent));
					}
					else
					{
						_source.Order(request.Source.IsGroup, Id(request.Source), request.InsertIndex);
					}

					Reload(Id(request.Source), true);
				}
			}
			catch (Exception e) { _error.Text = e.Message; }
		};
		Tree.ContextMenuRequested += (_, e) =>
		{
			foreach (Button button in _commands.Children.OfType<Button>().Where(b => b.Content?.ToString() != "Retry"))
			{
				MenuFlyoutItem item = new() { Text = button.Content.ToString(), IsEnabled = button.IsEnabled };
				item.Click += (_, _) => new Microsoft.UI.Xaml.Automation.Peers.ButtonAutomationPeer(button).Invoke();
				e.Menu.Items.Add(item);
			}
		};
		string key = catalog + (picker ? groupsOnly ? ".Groups" : ".Picker" : ".Main");
		Tree.ColumnWidthsApplying += (_, e) =>
		{
			try
			{
				_widths.Save(key, e.Widths.ToArray());
			}
			catch (Exception ex) { e.ErrorMessage = ex.Message; }
		};
		try
		{
			if (_widths.Load(key) is { } widths)
			{
				Tree.SetColumnWidths(widths);
			}
		}
		catch (Exception e) { _error.Text = e.Message; }

		Reload(null, false);
	}

	public static Guid Id(TreeGridNode node) => node.Data switch { GroupInfo g => g.Id, ElementInfo e => e.Id, _ => Guid.Empty };

	public void Reload(Guid? reveal, bool forceReveal)
	{
		try
		{
			TreeInfo data;
			if (_source.Name == "Accounts")
			{
				AccountTreeInfo accounts = _session.Host.Call<IAccountGroupService, AccountTreeInfo>(s => s.GetAccountsTree());
				data = new();
				data.Groups.AddRange(accounts.Groups);
				data.Elements.AddRange(accounts.Elements);
			}
			else
			{
				data = _source.Read();
			}

			reveal ??= Tree.SelectedNode is { } selected ? Id(selected) : null;
			Tree.UpdateRows(() =>
			{
				HashSet<Guid> live = data.Groups.Select(g => g.Id).Concat(_groupsOnly ? Array.Empty<Guid>() : data.Elements.Select(e => e.Id)).ToHashSet();
				foreach (Guid id in _nodes.Keys.Where(id => !live.Contains(id)).ToArray())
				{
					_nodes.Remove(id);
				}

				foreach (GroupInfo group in data.Groups)
				{
					if (!_nodes.TryGetValue(group.Id, out TreeGridNode? node))
					{
						_nodes[group.Id] = node = new() { IsExpanded = group.IsRoot };
					}

					node.CanEditStar = !_readOnly && !group.IsRoot;
					node.IsGroup = true;
					node.Data = group;
					node.Name = group.Name + (_groupsOnly ? $" ({data.Elements.Count(e => e.GroupId == group.Id)})" : "");
					node.IsStarred = group.IsFavorite;
					node.Children.Clear();
				}

				foreach (GroupInfo group in data.Groups.Where(g => !g.IsRoot).OrderBy(g => g.Order))
				{
					_nodes[group.ParentId].Children.Add(_nodes[group.Id]);
				}

				if (!_groupsOnly)
				{
					foreach (ElementInfo element in data.Elements.OrderBy(e => e.Order))
					{
						if (!_nodes.TryGetValue(element.Id, out TreeGridNode? node))
						{
							_nodes[element.Id] = node = new();
						}

						node.CanEditStar = !_readOnly;
						node.Data = element;
						node.Name = element.Name;
						node.IsStarred = element.IsFavorite;
						_nodes[element.GroupId].Children.Add(node);
					}
				}

				var roots = data.Groups.Where(g => g.IsRoot).Select(g => _nodes[g.Id]).ToList();
				if (!Tree.ItemsSource.SequenceEqual(roots))
				{
					Tree.ItemsSource = new ObservableCollection<TreeGridNode>(roots);
				}
			});
			if (reveal is Guid id && _nodes.TryGetValue(id, out TreeGridNode? target))
			{
				if (forceReveal)
				{
					Tree.StarredOnly = false;
				}

				Tree.RevealNode(target);
			}

			Ready = true;
			Tree.IsEnabled = true;
			_error.Text = "";
		}
		catch (Exception e)
		{
			Ready = false;
			Tree.IsEnabled = false;
			_error.Text = "Refresh failed. " + e.Message;
		}

		Commands();
	}

	private void Commands()
	{
		_commands.Children.Clear();
		_commands.Children.Add(Ui.Button("Retry", () => Reload(null, false), _error));
		if (_combineSource is { } source)
		{
			Button combine = Ui.AsyncButton("Combine into selected", async () =>
			{
				TreeGridNode target = Tree.SelectedNode ?? throw new InvalidOperationException("Choose a destination element.");
				if (target.IsGroup || Id(target) == Id(source))
				{
					return;
				}

				if (!await Ui.Confirm(_owner, $"Combine '{source.Name}' into '{target.Name}'? '{source.Name}' will be removed."))
				{
					return;
				}

				if (_source.Name == "Accounts")
				{
					_session.Coordinator.CheckAccountDelete(Id(source));
				}

				_source.Merge(false, Id(source), Id(target));
				_combineSource = null;
				Reload(Id(target), true);
			}, _error);
			combine.IsEnabled = Ready && Tree.SelectedNode is { IsGroup: false } target && Id(target) != Id(source)
			                    && (source.Data is not AccountElementInfo account || target.Data is AccountElementInfo other && account.CurrencyId == other.CurrencyId);
			_commands.Children.Add(combine);
			_commands.Children.Add(Ui.Button("Cancel combination", () =>
			{
				_combineSource = null;
				Commands();
			}, _error));
			return;
		}

		if (_readOnly)
		{
			return;
		}

		TreeGridNode? selected = Tree.SelectedNode;
		Guid? parent = selected?.Data switch { GroupInfo g => g.Id, ElementInfo e => e.GroupId, _ => null };
		_commands.Children.Add(Ui.AsyncButton("Add group", async () =>
		{
			Guid? id = await _session.EditCatalog(_owner, _source.Name, null, true, parent);
			Reload(id, id.HasValue);
		}, _error));
		if (!_groupsOnly)
		{
			_commands.Children.Add(Ui.AsyncButton("Add", async () =>
			{
				Guid? id = await _session.EditCatalog(_owner, _source.Name, null, false, parent);
				Reload(id, id.HasValue);
			}, _error));
		}

		Button edit = Ui.AsyncButton("Edit", () => Edit(Tree.SelectedNode), _error);
		edit.IsEnabled = selected is not null && selected.Data is not GroupInfo { IsRoot: true };
		_commands.Children.Add(edit);
		Button delete = Ui.AsyncButton("Delete", async () =>
		{
			if (selected is null || !await Ui.Confirm(_owner, $"Delete '{selected.Name}'?"))
			{
				return;
			}

			if (_source.Name == "Accounts" && !selected.IsGroup)
			{
				_session.Coordinator.CheckAccountDelete(Id(selected));
			}

			Guid? parentId = selected.Data is GroupInfo group ? group.ParentId : (selected.Data as ElementInfo)?.GroupId;
			_source.Delete(selected.IsGroup, Id(selected));
			Reload(parentId, false);
		}, _error);
		delete.IsEnabled = edit.IsEnabled;
		_commands.Children.Add(delete);
		foreach (bool merge in new[] { false, true })
		{
			if (merge && _picker)
			{
				continue;
			}

			Button move = Ui.AsyncButton(merge ? "Merge" : "Move", async () =>
			{
				if (selected is null)
				{
					return;
				}

				if (merge && !selected.IsGroup)
				{
					_combineSource = selected;
					Commands();
					return;
				}

				bool groups = !merge || selected.IsGroup;
				Guid? destination = await _session.Pick(_owner, _source.Name, groups, null, id => ValidDestination(selected, id));
				if (destination is null)
				{
					return;
				}

				if (merge && !await Ui.Confirm(_owner, $"Merge '{selected.Name}' into selected destination? Source will be removed."))
				{
					return;
				}

				if (merge)
				{
					_source.Merge(selected.IsGroup, Id(selected), destination.Value);
				}
				else
				{
					_source.Move(selected.IsGroup, Id(selected), destination.Value);
				}

				Reload(merge ? destination : Id(selected), true);
			}, _error);
			move.IsEnabled = edit.IsEnabled && !(merge && _source.Name == "Templates" && selected?.IsGroup == false);
			_commands.Children.Add(move);
		}

		if (!Ready)
		{
			foreach (Button button in _commands.Children.OfType<Button>().Skip(1))
			{
				button.IsEnabled = false;
			}
		}
	}

	private bool ValidDestination(TreeGridNode source, Guid candidate)
	{
		if (Id(source) == candidate)
		{
			return false;
		}

		if (!source.IsGroup)
		{
			return true;
		}

		if (!_nodes.TryGetValue(candidate, out TreeGridNode? node))
		{
			return false;
		}

		for (TreeGridNode? current = node; current is not null; current = current.Parent)
		{
			if (Id(current) == Id(source))
			{
				return false;
			}
		}

		return true;
	}

	private async Task Edit(TreeGridNode? node)
	{
		if (node is null || !Ready)
		{
			return;
		}

		try
		{
			Guid? id = await _session.EditCatalog(_owner, _source.Name, Id(node), node.IsGroup, null);
			Reload(id, id.HasValue);
		}
		catch (Exception e) { _error.Text = e.Message; }
	}
}
