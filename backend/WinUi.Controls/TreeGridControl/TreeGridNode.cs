using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Supplies a domain-independent row and its child hierarchy to TreeGrid.
/// The same instance retains expansion, star and checkbox state while hidden.
/// Children contain groups or leaves in the order supplied by the application.
/// Parent and depth are maintained by the visible-tree projection.
/// Data can hold an application view model for additional column bindings.
/// Checkbox state is independent of the selected row.
/// TreeGrid propagates user checkbox changes; the application owns persistence.
/// Instances and child collections are changed on the UI thread.
/// </summary>
public sealed partial class TreeGridNode : INotifyPropertyChanged
{
	private string _name = string.Empty;
	private bool _isExpanded;
	private bool _isStarred;
	private bool _canEditStar = true;
	private bool? _isChecked = false;
	private int _depth;
	private bool _isGroup;
	private object? _data;

	public string Name { get => _name; set => SetField(ref _name, value); }
	public bool IsGroup { get => _isGroup; set => SetField(ref _isGroup, value); }
	public object? Data { get => _data; set => SetField(ref _data, value); }
	public bool IsExpanded { get => _isExpanded; set => SetField(ref _isExpanded, value); }
	public bool CanEditStar { get => _canEditStar; set => SetField(ref _canEditStar, value); }
	public bool IsStarred { get => _isStarred; set => SetField(ref _isStarred, value); }
	public bool? IsChecked { get => _isChecked; set => SetField(ref _isChecked, value); }
	public int Depth { get => _depth; internal set => SetField(ref _depth, value); }
	public TreeGridNode? Parent { get; internal set; }
	public ObservableCollection<TreeGridNode> Children { get; } = new();
	public event PropertyChangedEventHandler? PropertyChanged;

	private void SetField<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
	{
		if (EqualityComparer<T>.Default.Equals(field, value))
		{
			return;
		}
		field = value;
		PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
	}
}
