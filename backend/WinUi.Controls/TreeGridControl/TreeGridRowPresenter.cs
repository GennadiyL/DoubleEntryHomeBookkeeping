using System.ComponentModel;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Data;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Renders one recycled TreeGrid row using the owner's column definitions.
/// The first cell contains indentation, expander, optional checkbox and name.
/// Additional cells bind to the row or its application-provided Data object.
/// The star button and checkbox do not activate the row.
/// Subscriptions follow the loaded lifetime and changing data context.
/// Actual hierarchy depth is retained while visual indentation stops at eight.
/// Public visibility is required by the control's XAML data template.
/// This presenter is an implementation detail rather than a standalone control.
/// </summary>
public sealed partial class TreeGridRowPresenter : UserControl
{
	private readonly SolidColorBrush _selectionBorderBrush = new(Windows.UI.Color.FromArgb(255, 96, 96, 96));
	private TreeGrid? _owner;
	private bool _pressedOnControl;
	private TreeGrid? _dragOwner;
	private TreeGridNode? _node;
	private Button? _expander;
	private TextBlock? _expanderGlyph;
	private Button? _star;
	private CheckBox? _check;
	private Grid? _nameArea;
	private Border? _selectionOutline;

	public TreeGridRowPresenter()
	{
		HorizontalContentAlignment = HorizontalAlignment.Stretch;
		IsTabStop = false;
		CanDrag = true;
		AddHandler(PointerPressedEvent, new PointerEventHandler(OnDragPointerPressed), true);
		DragStarting += (_, e) =>
		{
			if (_pressedOnControl || _node is null || _owner is null)
			{
				e.Cancel = true;
				return;
			}
			_dragOwner = _owner;
			_dragOwner.BeginDrag(_node, e);
		};
		DropCompleted += (_, _) =>
		{
			_dragOwner?.EndDrag();
			_dragOwner = null;
		};
		Loaded += OnLoaded;
		Unloaded += OnUnloaded;
		DataContextChanged += (_, _) => BindNode();
		Tapped += OnTapped;
		DoubleTapped += OnDoubleTapped;
	}

	private void OnLoaded(object sender, RoutedEventArgs e)
	{
		DependencyObject? parent = VisualTreeHelper.GetParent(this);
		while (parent is not null && parent is not TreeGrid)
		{
			parent = VisualTreeHelper.GetParent(parent);
		}
		_owner = parent as TreeGrid;
		if (_owner is not null)
		{
			_owner.LayoutChanged += OnLayoutChanged;
			_owner.SelectedNodeChanged += OnSelectedNodeChanged;
		}
		BindNode();
	}

	private void OnUnloaded(object sender, RoutedEventArgs e)
	{
		if (_owner is not null)
		{
			_owner.LayoutChanged -= OnLayoutChanged;
			_owner.SelectedNodeChanged -= OnSelectedNodeChanged;
		}
		if (_node is not null)
		{
			_node.PropertyChanged -= OnNodeChanged;
		}
		_owner = null;
		_node = null;
	}

	private void BindNode()
	{
		if (_node is not null)
		{
			_node.PropertyChanged -= OnNodeChanged;
		}
		_node = DataContext as TreeGridNode;
		if (_node is not null && _owner is not null)
		{
			_node.PropertyChanged += OnNodeChanged;
		}
		Render();
	}

	private void OnLayoutChanged(object? sender, EventArgs e) => Render();
	private void OnSelectedNodeChanged(object? sender, TreeGridNode? node) => UpdateSelection();
	private void OnNodeChanged(object? sender, PropertyChangedEventArgs e) => UpdateState();

	private void Render()
	{
		if (_owner is null || _node is null)
		{
			Content = null;
			return;
		}
		Grid row = new() { MinHeight = 28, Background = new SolidColorBrush(Microsoft.UI.Colors.Transparent) };
		_owner.ConfigureColumns(row);
		_nameArea = new Grid { VerticalAlignment = VerticalAlignment.Center };
		_nameArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_nameArea.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		_nameArea.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		_expander = new Button { Width = 28, Height = 24, MinHeight = 0, Padding = new Thickness(0), Background = null, BorderThickness = new Thickness(0) };
		_expanderGlyph = new TextBlock();
		_expander.Content = _expanderGlyph;
		_expander.Click += (_, _) => _node.IsExpanded = !_node.IsExpanded;
		_expander.DoubleTapped += StopDoubleTap;
		_nameArea.Children.Add(_expander);
		_check = new CheckBox { MinWidth = 0, MinHeight = 0, Height = 24, Padding = new Thickness(0), VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 0, 8, 0), IsThreeState = true };
		_check.Click += (_, _) =>
		{
			_owner.ToggleCheck(_node);
			UpdateState();
		};
		_check.DoubleTapped += StopDoubleTap;
		Grid.SetColumn(_check, 1);
		_nameArea.Children.Add(_check);
		TextBlock name = CreateText(nameof(TreeGridNode.Name), HorizontalAlignment.Stretch);
		name.Margin = new Thickness(4, 0, 8, 0);
		Grid.SetColumn(name, 2);
		_nameArea.Children.Add(name);
		row.Children.Add(_nameArea);
		for (int i = 0; i < _owner.Columns.Count; i++)
		{
			TreeGridColumn column = _owner.Columns[i];
			FrameworkElement cell = column.CellTemplate is null
				? CreateText(column.BindingPath, HorizontalAlignment.Stretch)
				: new ContentControl { Content = _node, ContentTemplate = column.CellTemplate, HorizontalContentAlignment = HorizontalAlignment.Stretch };
			Grid.SetColumn(cell, i + 1);
			row.Children.Add(cell);
		}
		_star = new Button { Width = 20, MinWidth = 0, Height = 24, MinHeight = 0, FontSize = 16, VerticalAlignment = VerticalAlignment.Center, Padding = new Thickness(0), Background = null, BorderThickness = new Thickness(0), HorizontalAlignment = HorizontalAlignment.Center };
		_star.Click += (_, _) => _owner.ToggleStar(_node);
		_star.DoubleTapped += StopDoubleTap;
		Grid.SetColumn(_star, _owner.Columns.Count + 1);
		row.Children.Add(_star);
		_selectionOutline = new Border { BorderThickness = new Thickness(1), IsHitTestVisible = false };
		_selectionOutline.BorderBrush = _selectionBorderBrush;
		Grid.SetColumnSpan(_selectionOutline, _owner.Columns.Count + 2);
		row.Children.Add(_selectionOutline);
		Content = row;
		UpdateSelection();
		UpdateState();
	}

	private TextBlock CreateText(string path, HorizontalAlignment alignment)
	{
		TextBlock text = new()
		{
			VerticalAlignment = VerticalAlignment.Center, HorizontalAlignment = alignment,
			TextTrimming = TextTrimming.CharacterEllipsis, TextWrapping = TextWrapping.NoWrap,
			TextAlignment = path == nameof(TreeGridNode.Name) ? TextAlignment.Left : TextAlignment.Center
		};
		text.SetBinding(TextBlock.TextProperty, new Binding { Source = _node, Path = new PropertyPath(path), Mode = BindingMode.OneWay });
		ToolTipService.SetToolTip(text, new ToolTip());
		((ToolTip)ToolTipService.GetToolTip(text)).SetBinding(ContentControl.ContentProperty,
			new Binding { Source = _node, Path = new PropertyPath(path), Mode = BindingMode.OneWay });
		return text;
	}

	private void UpdateState()
	{
		if (_node is null || _owner is null || _expander is null || _expanderGlyph is null || _star is null || _check is null || _nameArea is null)
		{
			return;
		}
		_nameArea.Margin = new Thickness(Math.Min(_node.Depth, 8) * 16, 0, 0, 0);
		_expanderGlyph.Text = _node.IsExpanded ? "⌄" : "›";
		_expanderGlyph.RenderTransform = new TranslateTransform { Y = _node.IsExpanded ? -3 : -1 };
		_expander.FontSize = _node.IsExpanded ? 13 : 18;
		_expander.Opacity = _node.IsGroup ? 1 : 0;
		_expander.IsHitTestVisible = _node.IsGroup;
		_expander.IsTabStop = _node.IsGroup;
		AutomationProperties.SetName(_expander, (_node.IsExpanded ? "Collapse " : "Expand ") + _node.Name);
		_check.Visibility = _owner.ShowCheckboxes ? Visibility.Visible : Visibility.Collapsed;
		_check.IsChecked = _node.IsChecked;
		AutomationProperties.SetName(_check, "Check " + _node.Name);
		_star.IsEnabled = _node.CanEditStar;
		_star.Content = _node.IsStarred ? "★" : "☆";
		AutomationProperties.SetName(_star, (_node.IsStarred ? "Remove star " : "Add star ") + _node.Name);
		ToolTipService.SetToolTip(_star, _node.IsStarred ? "Remove star" : "Add star");
	}

	private void UpdateSelection()
	{
		if (_selectionOutline is not null)
		{
			_selectionOutline.Visibility = _node is not null && ReferenceEquals(_owner?.SelectedNode, _node)
				? Visibility.Visible
				: Visibility.Collapsed;
		}
	}

	private void OnDragPointerPressed(object sender, PointerRoutedEventArgs e)
	{
		_pressedOnControl = false;
		DependencyObject? source = e.OriginalSource as DependencyObject;
		while (source is not null && source != this)
		{
			if (source is ButtonBase or TextBox or PasswordBox or ComboBox or Slider)
			{
				_pressedOnControl = true;
				break;
			}
			source = VisualTreeHelper.GetParent(source);
		}
	}

	private void OnTapped(object sender, TappedRoutedEventArgs e)
	{
		DependencyObject? source = e.OriginalSource as DependencyObject;
		while (source is not null && source != this)
		{
			if (source is ButtonBase or TextBox or PasswordBox or ComboBox or Slider)
			{
				return;
			}
			source = VisualTreeHelper.GetParent(source);
		}
		if (_node is not null && _owner is not null)
		{
			_owner.SelectFromPointer(_node);
			e.Handled = true;
		}
	}

	private void OnDoubleTapped(object sender, DoubleTappedRoutedEventArgs e)
	{
		if (_node is not null)
		{
			_owner?.Activate(_node);
			e.Handled = true;
		}
	}

	private void StopDoubleTap(object sender, DoubleTappedRoutedEventArgs e) => e.Handled = true;
}
