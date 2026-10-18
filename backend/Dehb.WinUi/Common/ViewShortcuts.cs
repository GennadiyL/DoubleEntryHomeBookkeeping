using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi.Common;

/// <summary>
/// Registers unmodified view navigation keys on the active visual surface.
/// F5 focuses the first enabled focusable control in visual tab order.
/// F6 exposes tree roots and their children while collapsing all descendants.
/// F7 expands the complete hierarchy, including previously hidden groups.
/// F8 delegates focus to the view's primary grid or grid-switching action.
/// Detached views and open flyouts do not process these shortcuts.
/// List focus restores the selected row or selects the first available row.
/// This helper does not change business data or register system-wide hotkeys.
/// </summary>
internal static class ViewShortcuts
{
	public static void Attach(FrameworkElement view, Action? focusGrid = null, TreeGrid? tree = null)
	{
		void Add(VirtualKey key, Action action)
		{
			KeyboardAccelerator shortcut = new() { Key = key, Modifiers = VirtualKeyModifiers.None, IsEnabled = view.IsLoaded };
			view.Loaded += (_, _) => shortcut.IsEnabled = true;
			view.Unloaded += (_, _) => shortcut.IsEnabled = false;
			shortcut.Invoked += (_, args) =>
			{
				if (!view.IsLoaded || VisualTreeHelper.GetParent(view) is null ||
					view.Visibility != Visibility.Visible || view.XamlRoot is null ||
					VisualTreeHelper.GetOpenPopupsForXamlRoot(view.XamlRoot).Any(popup => popup.IsLightDismissEnabled || popup.Child is ContentDialog))
				{
					return;
				}
				args.Handled = true;
				action();
			};
			view.KeyboardAccelerators.Add(shortcut);
		}
		Add(VirtualKey.F5, () => FocusFirst(view));
		if (tree is not null)
		{
			Add(VirtualKey.F6, () => tree.SetAllExpanded(false));
			Add(VirtualKey.F7, () => tree.SetAllExpanded(true));
		}
		if (focusGrid is not null)
		{
			Add(VirtualKey.F8, () => view.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (view.IsLoaded && VisualTreeHelper.GetParent(view) is not null)
				{
					focusGrid();
				}
			}));
		}
	}

	public static void FocusFirst(FrameworkElement view)
	{
		if (view.IsLoaded && FocusManager.FindFirstFocusableElement(view) is Control control)
		{
			control.Focus(FocusState.Keyboard);
		}
	}

	public static void FocusRows(ListView list)
	{
		if (!list.IsEnabled)
		{
			return;
		}
		if (list.SelectedIndex < 0 && list.Items.Count > 0)
		{
			list.SelectedIndex = 0;
		}
		if (list.SelectedItem is { } selected)
		{
			list.ScrollIntoView(selected);
			list.UpdateLayout();
			if (list.ContainerFromItem(selected) is ListViewItem container)
			{
				if (container.Focus(FocusState.Keyboard))
				{
					return;
				}
			}
		}
		list.Focus(FocusState.Keyboard);
		if (list.SelectedItem is { } pending)
		{
			list.DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
			{
				if (list.IsLoaded && list.IsEnabled && ReferenceEquals(list.SelectedItem, pending))
				{
					list.UpdateLayout();
					if (list.ContainerFromItem(pending) is ListViewItem row)
					{
						row.Focus(FocusState.Keyboard);
					}
				}
			});
		}
	}
}
