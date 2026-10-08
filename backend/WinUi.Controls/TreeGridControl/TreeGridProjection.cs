using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Maintains the visible preorder projection of a complete row hierarchy.
/// Observes structural changes and group expansion while attached.
/// Hidden descendants retain their own expansion and checkbox values.
/// Existing visible row instances survive projection updates.
/// Parent links and actual depth include collapsed descendants.
/// Rejects cycles, shared nodes and children attached to leaves.
/// Detaching releases subscriptions when the owning control is unloaded.
/// This component has no dependency on WinUI or application services.
/// </summary>
internal sealed class TreeGridProjection
{
	private ObservableCollection<TreeGridNode>? _roots;
	private readonly HashSet<TreeGridNode> _observed = new();
	public ObservableCollection<TreeGridNode> VisibleRows { get; } = new();
	public event EventHandler? Changing;
	public event EventHandler? Changed;

	public void Attach(ObservableCollection<TreeGridNode> roots)
	{
		Detach();
		_roots = roots;
		_roots.CollectionChanged += OnCollectionChanged;
		Refresh();
	}

	public void Detach()
	{
		if (_roots is not null)
		{
			_roots.CollectionChanged -= OnCollectionChanged;
		}
		foreach (TreeGridNode node in _observed)
		{
			node.PropertyChanged -= OnNodeChanged;
			node.Children.CollectionChanged -= OnCollectionChanged;
		}
		_observed.Clear();
		_roots = null;
	}

	private void OnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

	private void OnNodeChanged(object? sender, PropertyChangedEventArgs e)
	{
		if (e.PropertyName is nameof(TreeGridNode.IsExpanded) or nameof(TreeGridNode.IsGroup))
		{
			Refresh();
		}
	}

	private void Refresh()
	{
		if (_roots is null)
		{
			return;
		}
		List<TreeGridNode> visible = new();
		HashSet<TreeGridNode> visited = new();
		Stack<(TreeGridNode Node, TreeGridNode? Parent, int Depth, bool Visible)> pending = new();
		for (int i = _roots.Count - 1; i >= 0; i--)
		{
			pending.Push((_roots[i], null, 0, true));
		}
		while (pending.TryPop(out (TreeGridNode Node, TreeGridNode? Parent, int Depth, bool Visible) item))
		{
			if (!visited.Add(item.Node))
			{
				throw new InvalidOperationException("A tree node cannot have multiple parents or form a cycle.");
			}
			if (!item.Node.IsGroup && item.Node.Children.Count != 0)
			{
				throw new InvalidOperationException("Only group nodes can contain children.");
			}
			item.Node.Parent = item.Parent;
			item.Node.Depth = item.Depth;
			if (item.Visible)
			{
				visible.Add(item.Node);
			}
			for (int i = item.Node.Children.Count - 1; i >= 0; i--)
			{
				pending.Push((item.Node.Children[i], item.Node, item.Depth + 1, item.Visible && item.Node.IsExpanded));
			}
		}
		foreach (TreeGridNode node in _observed.Except(visited).ToArray())
		{
			node.PropertyChanged -= OnNodeChanged;
			node.Children.CollectionChanged -= OnCollectionChanged;
			_observed.Remove(node);
		}
		foreach (TreeGridNode node in visited.Except(_observed).ToArray())
		{
			node.PropertyChanged += OnNodeChanged;
			node.Children.CollectionChanged += OnCollectionChanged;
			_observed.Add(node);
		}
		Changing?.Invoke(this, EventArgs.Empty);
		for (int i = 0; i < visible.Count; i++)
		{
			if (i < VisibleRows.Count && ReferenceEquals(VisibleRows[i], visible[i]))
			{
				continue;
			}
			int existing = VisibleRows.IndexOf(visible[i]);
			if (existing >= 0)
			{
				VisibleRows.Move(existing, i);
			}
			else
			{
				VisibleRows.Insert(i, visible[i]);
			}
		}
		while (VisibleRows.Count > visible.Count)
		{
			VisibleRows.RemoveAt(VisibleRows.Count - 1);
		}
		Changed?.Invoke(this, EventArgs.Empty);
	}
}
