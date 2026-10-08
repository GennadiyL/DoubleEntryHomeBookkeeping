using System.Collections.ObjectModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Displays a reusable, virtualized hierarchy with aligned data columns.
/// The name and favorite cells surround application-defined additional columns.
/// A separate visible projection preserves collapsed descendants and their state.
/// Selection remains independent of optional three-state checkboxes.
/// Row activation and user changes are exposed to the hosting application.
/// Domain validation, checkbox propagation and persistence belong to that host.
/// Header and row widths share the same proportional column definitions.
/// Public visibility permits construction from application XAML.
/// </summary>
public sealed partial class TreeGrid : UserControl
{
	public static readonly DependencyProperty ItemsSourceProperty = DependencyProperty.Register(
		nameof(ItemsSource), typeof(ObservableCollection<TreeGridNode>), typeof(TreeGrid), new PropertyMetadata(null, OnItemsSourceChanged));
	public static readonly DependencyProperty ShowCheckboxesProperty = DependencyProperty.Register(
		nameof(ShowCheckboxes), typeof(bool), typeof(TreeGrid), new PropertyMetadata(false, OnLayoutPropertyChanged));
	public static readonly DependencyProperty SelectedNodeProperty = DependencyProperty.Register(
		nameof(SelectedNode), typeof(TreeGridNode), typeof(TreeGrid), new PropertyMetadata(null, OnSelectedNodeChanged));
	public static readonly DependencyProperty NameHeaderProperty = DependencyProperty.Register(
		nameof(NameHeader), typeof(string), typeof(TreeGrid), new PropertyMetadata("Name", OnLayoutPropertyChanged));

	private readonly TreeGridProjection _projection = new();
	private bool _changingProjection;
	private bool _attached;
	private TreeGridNode? _selectionBeforeRefresh;

	public ObservableCollection<TreeGridNode> ItemsSource
	{
		get => (ObservableCollection<TreeGridNode>)GetValue(ItemsSourceProperty);
		set => SetValue(ItemsSourceProperty, value);
	}
	public bool ShowCheckboxes { get => (bool)GetValue(ShowCheckboxesProperty); set => SetValue(ShowCheckboxesProperty, value); }
	public TreeGridNode? SelectedNode { get => (TreeGridNode?)GetValue(SelectedNodeProperty); set => SetValue(SelectedNodeProperty, value); }
	public string NameHeader { get => (string)GetValue(NameHeaderProperty); set => SetValue(NameHeaderProperty, value); }
	public ObservableCollection<TreeGridColumn> Columns { get; } = new();
	public event EventHandler<TreeGridNode>? RowActivated;
	public event EventHandler<TreeGridNode>? FavoriteChanged;
	public event EventHandler<TreeGridNode>? CheckStateChanged;
	public event EventHandler<TreeGridNode?>? SelectedNodeChanged;
	internal event EventHandler? LayoutChanged;

	public TreeGrid()
	{
		InitializeComponent();
		RowsList.ItemsSource = _projection.VisibleRows;
		ItemsSource = new ObservableCollection<TreeGridNode>();
		Columns.CollectionChanged += (_, _) => RefreshLayout();
		_projection.Changing += (_, _) =>
		{
			_selectionBeforeRefresh = SelectedNode;
			_changingProjection = true;
		};
		_projection.Changed += (_, _) =>
		{
			TreeGridNode? selection = _selectionBeforeRefresh;
			while (selection is not null && !_projection.VisibleRows.Contains(selection))
			{
				selection = selection.Parent;
			}
			_changingProjection = false;
			SelectedNode = selection;
			RowsList.SelectedItem = selection;
		};
		Loaded += (_, _) =>
		{
			_attached = true;
			_projection.Attach(ItemsSource);
			RefreshLayout();
		};
		Unloaded += (_, _) =>
		{
			_attached = false;
			_projection.Detach();
		};
		RefreshLayout();
	}

	internal void ConfigureColumns(Grid grid)
	{
		grid.ColumnDefinitions.Clear();
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(7, GridUnitType.Star), MinWidth = 280 });
		foreach (TreeGridColumn column in Columns)
		{
			if (!double.IsFinite(column.Width) || column.Width <= 0)
			{
				throw new InvalidOperationException("Column width must be finite and positive.");
			}
			grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(column.Width, GridUnitType.Star), MinWidth = 60 });
		}
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star), MinWidth = 64 });
	}

	internal void Activate(TreeGridNode node)
	{
		SelectedNode = node;
		RowActivated?.Invoke(this, node);
	}

	internal void ToggleFavorite(TreeGridNode node)
	{
		node.IsFavorite = !node.IsFavorite;
		FavoriteChanged?.Invoke(this, node);
	}

	internal void ToggleCheck(TreeGridNode node)
	{
		node.IsChecked = node.IsChecked != true;
		CheckStateChanged?.Invoke(this, node);
	}

	private static void OnItemsSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		TreeGrid tree = (TreeGrid)sender;
		if (tree._attached)
		{
			tree._projection.Attach(tree.ItemsSource);
		}
	}

	private static void OnLayoutPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e) => ((TreeGrid)sender).RefreshLayout();

	private static void OnSelectedNodeChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		TreeGrid tree = (TreeGrid)sender;
		tree.RowsList.SelectedItem = e.NewValue;
		tree.SelectedNodeChanged?.Invoke(tree, (TreeGridNode?)e.NewValue);
	}

	private void RefreshLayout()
	{
		if (HeaderGrid is null)
		{
			return;
		}
		ConfigureColumns(HeaderGrid);
		HeaderGrid.Children.Clear();
		AddHeader(NameHeader, 0, HorizontalAlignment.Left);
		for (int i = 0; i < Columns.Count; i++)
		{
			AddHeader(Columns[i].Header, i + 1, HorizontalAlignment.Center);
		}
		AddHeader("Favorite", Columns.Count + 1, HorizontalAlignment.Center);
		MinWidth = 360 + Columns.Count * 60;
		LayoutChanged?.Invoke(this, EventArgs.Empty);
	}

	private void AddHeader(string text, int column, HorizontalAlignment alignment)
	{
		TextBlock header = new() { Text = text, HorizontalAlignment = alignment, VerticalAlignment = VerticalAlignment.Center };
		Grid.SetColumn(header, column);
		HeaderGrid.Children.Add(header);
	}

	private void OnSelectionChanged(object sender, SelectionChangedEventArgs e)
	{
		if (!_changingProjection)
		{
			SelectedNode = RowsList.SelectedItem as TreeGridNode;
		}
	}

	private void OnKeyDown(object sender, KeyRoutedEventArgs e)
	{
		DependencyObject? source = e.OriginalSource as DependencyObject;
		while (source is not null && source is not ListViewItem && source != RowsList)
		{
			if (source is TextBox or ButtonBase)
			{
				return;
			}
			source = VisualTreeHelper.GetParent(source);
		}
		if (SelectedNode is not TreeGridNode node || _projection.VisibleRows.Count == 0)
		{
			return;
		}
		switch (e.Key)
		{
			case VirtualKey.Right:
				if (node.IsGroup && !node.IsExpanded)
				{
					node.IsExpanded = true;
				}
				else if (node.Children.Count > 0)
				{
					SelectAndReveal(node.Children[0]);
				}
				break;
			case VirtualKey.Left:
				if (node.IsGroup && node.IsExpanded)
				{
					node.IsExpanded = false;
				}
				else if (node.Parent is not null)
				{
					SelectAndReveal(node.Parent);
				}
				break;
			case VirtualKey.Home:
				SelectAndReveal(_projection.VisibleRows[0]);
				break;
			case VirtualKey.End:
				SelectAndReveal(_projection.VisibleRows[^1]);
				break;
			case VirtualKey.Enter:
				Activate(node);
				break;
			case VirtualKey.Space when ShowCheckboxes:
				ToggleCheck(node);
				break;
			default:
				return;
		}
		e.Handled = true;
	}

	private void SelectAndReveal(TreeGridNode node)
	{
		SelectedNode = node;
		RowsList.ScrollIntoView(node);
	}
}
