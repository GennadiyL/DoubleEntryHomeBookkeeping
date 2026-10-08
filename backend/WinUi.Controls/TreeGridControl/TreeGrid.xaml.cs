using System.Collections.ObjectModel;
using System.Diagnostics;
using Windows.ApplicationModel.DataTransfer;
using Windows.ApplicationModel.DataTransfer.DragDrop;
using Windows.Foundation;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Displays a reusable, virtualized hierarchy with aligned data columns.
/// The name and star cells surround application-defined additional columns.
/// A separate visible projection preserves collapsed descendants and their state.
/// Selection remains independent of optional three-state checkboxes.
/// Row activation and user changes are exposed to the hosting application.
/// Domain validation and persistence belong to that host.
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

	public static readonly DependencyProperty AllowDragDropProperty = DependencyProperty.Register(
		nameof(AllowDragDrop), typeof(bool), typeof(TreeGrid), new PropertyMetadata(false));
	public static readonly DependencyProperty AllowMergeProperty = DependencyProperty.Register(
		nameof(AllowMerge), typeof(bool), typeof(TreeGrid), new PropertyMetadata(false));

	private readonly DispatcherTimer _dragTimer = new() { Interval = TimeSpan.FromMilliseconds(100) };
	private readonly Stopwatch _hoverWatch = new();
	private TreeGridNode? _dragSource;
	private TreeGridNode? _hoverTarget;
	private string? _dragToken;
	private string _dropReason = "Not allowed";
	private Point _dragPoint;
	private bool _dragShift;
	private ScrollViewer? _dragScrollViewer;

	private readonly TreeGridProjection _projection = new();
	private bool _changingProjection;
	private bool _attached;
	private TreeGridNode? _selectionBeforeRefresh;

	public bool AllowDragDrop { get => (bool)GetValue(AllowDragDropProperty); set => SetValue(AllowDragDropProperty, value); }
	public bool AllowMerge { get => (bool)GetValue(AllowMergeProperty); set => SetValue(AllowMergeProperty, value); }
	public event EventHandler<TreeGridDropValidationEventArgs>? DropValidating;
	public event EventHandler<TreeGridDropRequest>? DropRequested;

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
	public event EventHandler<TreeGridNode>? StarChanged;
	public event EventHandler<TreeGridNode>? CheckStateChanged;
	public event EventHandler<TreeGridNode?>? SelectedNodeChanged;
	internal event EventHandler? LayoutChanged;

	public TreeGrid()
	{
		InitializeComponent();
		_dragTimer.Tick += OnDragTimerTick;
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
			EndDrag();
		};
		RefreshLayout();
	}

	public void UpdateRows(Action update) => _projection.UpdateRows(update);

	public void RevealNode(TreeGridNode node)
	{
		for (TreeGridNode? parent = node.Parent; parent is not null; parent = parent.Parent)
		{
			parent.IsExpanded = true;
		}
		SelectAndReveal(node);
	}

	internal void BeginDrag(TreeGridNode node, DragStartingEventArgs e)
	{
		if (!AllowDragDrop || node.Parent is null || DropRequested is null)
		{
			e.Cancel = true;
			return;
		}
		EndDrag();
		_dragSource = node;
		_dragToken = Guid.NewGuid().ToString();
		e.Data.Properties["TreeGridDragToken"] = _dragToken;
		e.Data.SetData("application/x-treegrid-row", _dragToken);
		e.Data.RequestedOperation = DataPackageOperation.Move;
		_dragScrollViewer = FindDragScrollViewer(RowsList);
		SelectedNode = node;
	}

	internal void EndDrag()
	{
		_dragTimer.Stop();
		_hoverWatch.Reset();
		_hoverTarget = null;
		_dragSource = null;
		_dragToken = null;
		_dragScrollViewer = null;
		DropMarker.Visibility = Visibility.Collapsed;
	}

	private bool IsOwnDrag(DragEventArgs e) => AllowDragDrop && _dragSource is not null
		&& e.DataView.Properties.TryGetValue("TreeGridDragToken", out object? token)
		&& Equals(token, _dragToken);

	private void OnRowsDragOver(object sender, DragEventArgs e)
	{
		e.Handled = true;
		e.AcceptedOperation = DataPackageOperation.None;
		if (!IsOwnDrag(e))
		{
			return;
		}
		_dragPoint = e.GetPosition(RowsList);
		_dragShift = (e.Modifiers & DragDropModifiers.Shift) != 0;
		TreeGridDropRequest? request = GetDropRequest(out ListViewItem? target, out TreeGridDropPosition position);
		ShowDropFeedback(request, target, position);
		e.DragUIOverride.IsCaptionVisible = true;
		e.DragUIOverride.Caption = request?.Operation.ToString() ?? _dropReason;
		e.AcceptedOperation = request is null ? DataPackageOperation.None : DataPackageOperation.Move;
		_dragTimer.Start();
	}

	private TreeGridDropRequest? GetDropRequest(out ListViewItem? target, out TreeGridDropPosition position)
	{
		_dropReason = "Not allowed";
		target = null;
		position = TreeGridDropPosition.Undefined;
		if (_dragSource is null || !AllowDragDrop || DropRequested is null
			|| _dragPoint.X < 0 || _dragPoint.X > RowsList.ActualWidth - 20
			|| _dragPoint.Y < 0 || _dragPoint.Y > RowsList.ActualHeight)
		{
			SetHoverTarget(null);
			return null;
		}
		if (RowsList.ItemsPanelRoot is null)
		{
			return null;
		}
		foreach (ListViewItem container in RowsList.ItemsPanelRoot.Children.OfType<ListViewItem>())
		{
			Point top = container.TransformToVisual(RowsList).TransformPoint(new Point());
			if (_dragPoint.Y >= top.Y && _dragPoint.Y < top.Y + container.ActualHeight)
			{
				target = container;
				double localY = _dragPoint.Y - top.Y;
				position = localY < container.ActualHeight * 0.25 ? TreeGridDropPosition.Before
					: localY > container.ActualHeight * 0.75 ? TreeGridDropPosition.After : TreeGridDropPosition.Inside;
				break;
			}
		}
		if (target?.Content is not TreeGridNode node)
		{
			SetHoverTarget(null);
			return null;
		}
		SetHoverTarget(position == TreeGridDropPosition.Inside && node.IsGroup ? node : null);
		TreeGridDropRequest? request = TreeGridDropPlanner.Create(_dragSource, node, position, _dragShift, AllowMerge, _projection.VisibleRows);
		if (request is null)
		{
			return null;
		}
		TreeGridDropValidationEventArgs validation = new() { Request = request };
		DropValidating?.Invoke(this, validation);
		if (validation.Cancel && !string.IsNullOrWhiteSpace(validation.Reason))
		{
			_dropReason = validation.Reason;
		}
		return validation.Cancel ? null : request;
	}

	private void ShowDropFeedback(TreeGridDropRequest? request, ListViewItem? target, TreeGridDropPosition position)
	{
		DropMarker.Visibility = Visibility.Collapsed;
		if (request is null || target is null)
		{
			return;
		}
		double y = target.TransformToVisual(RowsList).TransformPoint(new Point()).Y;
		if (position == TreeGridDropPosition.After)
		{
			y += target.ActualHeight - 2;
		}
		DropMarker.Height = position == TreeGridDropPosition.Inside ? target.ActualHeight : 2;
		DropMarker.Margin = new Thickness(8, Math.Max(0, y), 28, 0);
		DropMarker.Visibility = Visibility.Visible;
	}

	private void SetHoverTarget(TreeGridNode? node)
	{
		if (ReferenceEquals(node, _hoverTarget))
		{
			return;
		}
		_hoverTarget = node;
		_hoverWatch.Restart();
	}

	private void OnDragTimerTick(object? sender, object e)
	{
		if (_dragSource is null || !AllowDragDrop)
		{
			EndDrag();
			return;
		}
		if (_hoverTarget is { IsExpanded: false } && _hoverWatch.ElapsedMilliseconds >= 700)
		{
			_hoverTarget.IsExpanded = true;
		}
		double delta = _dragPoint.X < 0 || _dragPoint.X > RowsList.ActualWidth - 20 ? 0
			: _dragPoint.Y < 28 ? -14 : _dragPoint.Y > RowsList.ActualHeight - 28 ? 14 : 0;
		if (delta != 0 && _dragScrollViewer is not null)
		{
			_dragScrollViewer.ChangeView(null, Math.Clamp(_dragScrollViewer.VerticalOffset + delta, 0, _dragScrollViewer.ScrollableHeight), null, true);
		}
		TreeGridDropRequest? request = GetDropRequest(out ListViewItem? target, out TreeGridDropPosition position);
		ShowDropFeedback(request, target, position);
	}

	private void OnRowsDragLeave(object sender, DragEventArgs e)
	{
		Point point = e.GetPosition(RowsList);
		if (point.X < 0 || point.Y < 0 || point.X >= RowsList.ActualWidth || point.Y >= RowsList.ActualHeight)
		{
			_dragTimer.Stop();
			SetHoverTarget(null);
			DropMarker.Visibility = Visibility.Collapsed;
		}
	}

	private void OnRowsDrop(object sender, DragEventArgs e)
	{
		e.Handled = true;
		e.AcceptedOperation = DataPackageOperation.None;
		if (!IsOwnDrag(e))
		{
			return;
		}
		_dragPoint = e.GetPosition(RowsList);
		_dragShift = (e.Modifiers & DragDropModifiers.Shift) != 0;
		TreeGridDropRequest? request = GetDropRequest(out _, out _);
		EndDrag();
		if (request is not null)
		{
			e.AcceptedOperation = DataPackageOperation.Move;
			DropRequested?.Invoke(this, request);
		}
	}

	private static ScrollViewer? FindDragScrollViewer(DependencyObject parent)
	{
		if (parent is ScrollViewer viewer)
		{
			return viewer;
		}
		for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
		{
			ScrollViewer? child = FindDragScrollViewer(VisualTreeHelper.GetChild(parent, i));
			if (child is not null)
			{
				return child;
			}
		}
		return null;
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
		grid.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(36) });
	}

	internal void SelectFromPointer(TreeGridNode node)
	{
		SelectedNode = node;
		if (RowsList.ContainerFromItem(node) is ListViewItem container)
		{
			container.Focus(FocusState.Pointer);
		}
	}

	internal void Activate(TreeGridNode node)
	{
		SelectedNode = node;
		RowActivated?.Invoke(this, node);
	}

	internal void ToggleStar(TreeGridNode node)
	{
		node.IsStarred = !node.IsStarred;
		StarChanged?.Invoke(this, node);
	}

	internal void ToggleCheck(TreeGridNode node)
	{
		TreeGridCheckState.Toggle(node);
		CheckStateChanged?.Invoke(this, node);
	}

	private static void OnItemsSourceChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
	{
		TreeGrid tree = (TreeGrid)sender;
		tree.EndDrag();
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
		AddHeader(NameHeader, 0, HorizontalAlignment.Left, 14);
		for (int i = 0; i < Columns.Count; i++)
		{
			AddHeader(Columns[i].Header, i + 1, HorizontalAlignment.Center, 14);
		}
		AddHeader("★", Columns.Count + 1, HorizontalAlignment.Center, 24);
		MinWidth = 352 + Columns.Count * 60;
		LayoutChanged?.Invoke(this, EventArgs.Empty);
	}

	private void AddHeader(string text, int column, HorizontalAlignment alignment, int fontSize)
	{
		TextBlock header = new()
		{
			Text = text,
			HorizontalAlignment = alignment,
			VerticalAlignment = VerticalAlignment.Center,
			FontSize = fontSize
		};
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

	private void OnRowsPreviewKeyDown(object sender, KeyRoutedEventArgs e)
	{
		if (_dragSource is not null && e.Key == VirtualKey.Escape)
		{
			EndDrag();
			e.Handled = true;
			return;
		}
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
