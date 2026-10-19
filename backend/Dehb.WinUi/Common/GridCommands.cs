using Microsoft.UI.Input;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Automation;
using Microsoft.UI.Xaml.Automation.Peers;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Media;
using Windows.System;
using Windows.UI.Core;
using WinUi.Controls.TreeGridControl;

namespace Dehb.WinUi.Common;

/// <summary>
/// Connects grid editing shortcuts to the existing toolbar commands.
/// Commands are resolved at invocation time because toolbars can be rebuilt.
/// Disabled commands retain the same restrictions as mouse interaction.
/// Text editors keep their normal Insert and Delete clipboard shortcuts.
/// Enter remains local to grid rows; search and picker activation are preserved.
/// Context menus and toolbar hints share the displayed shortcut names.
/// Other commands are available throughout the active view or selection popup.
/// This helper does not implement persistence or business validation.
/// </summary>
internal static class GridCommands
{
	public static string Shortcut(string label) => label switch
	{
		"Refresh" => "Ctrl+R",
		"Duplicate" => "Ctrl+D",
		"From template" => "Ctrl+L",
		"Create template" => "Ctrl+Shift+L",
		"Today" => "Ctrl+T",
		"Next" => "Ctrl+PageDown",
		"Previous" => "Ctrl+PageUp",
		"Choose account" => "Ctrl+9",
		"Clear account" => "Ctrl+Shift+9",
		"Move" => "Ctrl+M",
		"Merge" or "Combine into selected" => "Ctrl+U",
		"Add star" or "Remove star" => "Ctrl+S",
		"Add group" or "Add currency" => "Ctrl+Shift+Insert",
		"Add" or "Add currency rate" => "Alt+Shift+Insert",
		"Edit" or "Edit currency" or "Edit currency rate" => "Shift+Enter",
		"Delete currency" => "Ctrl+Shift+Delete",
		"Delete" or "Delete currency rates" => "Alt+Shift+Delete",
		_ => ""
	};

	public static string Hint(string label) => Shortcut(label) is { Length: > 0 } shortcut ? $"{label} ({shortcut})" : label;

	public static MenuFlyoutItem MenuItem(Button button)
	{
		string label = AutomationProperties.GetName(button);
		MenuFlyoutItem item = new() { Text = label, IsEnabled = button.IsEnabled, KeyboardAcceleratorTextOverride = Shortcut(label) };
		item.Click += (_, _) => Invoke(button);
		return item;
	}

	public static void Attach(FrameworkElement grid, Func<IEnumerable<Button>> commands, string editLabel = "Edit", bool currency = false, bool activateSelection = false, Func<bool>? canStar = null, FrameworkElement? scope = null)
	{
		(scope ?? grid).PreviewKeyDown += (_, args) =>
		{
			if (args.Handled || !grid.IsLoaded || grid is Control { IsEnabled: false }) { return; }
			if (args.Key == VirtualKey.Enter)
			{
				DependencyObject? source = args.OriginalSource as DependencyObject;
				while (source is not null && source != grid)
				{
					if (source is TextBox or ComboBox or Microsoft.UI.Xaml.Controls.Primitives.ButtonBase) { return; }
					source = VisualTreeHelper.GetParent(source);
				}
				if (source != grid) { return; }
			}
			if (grid.XamlRoot is null || VisualTreeHelper.GetOpenPopupsForXamlRoot(grid.XamlRoot).Any(popup => popup.IsLightDismissEnabled || popup.Child is ContentDialog)) { return; }
			bool shift = Down(VirtualKey.Shift);
			bool control = Down(VirtualKey.Control);
			bool alt = Down(VirtualKey.Menu);
			if (Down(VirtualKey.LeftWindows) || Down(VirtualKey.RightWindows)) { return; }
			if (control && !shift && !alt && args.Key == VirtualKey.S && grid is TreeGrid tree)
			{
				args.Handled = true;
				if (canStar?.Invoke() != false) { tree.ToggleSelectedStar(); }
				return;
			}
			if (activateSelection && args.Key == VirtualKey.Enter && !shift && !control && !alt) { return; }
			string? label = args.Key switch
			{
				VirtualKey.R when control && !shift && !alt => "Refresh",
				VirtualKey.M when control && !shift && !alt => "Move",
				VirtualKey.U when control && !shift && !alt => commands().Any(command => AutomationProperties.GetName(command) == "Combine into selected") ? "Combine into selected" : "Merge",
				VirtualKey.Insert when shift && control && !alt => currency ? "Add currency" : "Add group",
				VirtualKey.Insert when shift && alt && !control => currency ? "Add currency rate" : "Add",
				VirtualKey.Delete when shift && control && !alt && currency => "Delete currency",
				VirtualKey.Delete when shift && alt && !control => currency ? "Delete currency rates" : "Delete",
				VirtualKey.Enter when shift && !control && !alt => editLabel,
				_ => null
			};
			Button? button = commands().FirstOrDefault(command => AutomationProperties.GetName(command) == label);
			if (args.Key == VirtualKey.Enter && !control && !alt) { args.Handled = true; }
			if (button is null) { return; }
			args.Handled = true;
			Invoke(button);
		};
	}

	public static void AddStarMenu(MenuFlyout menu, TreeGrid tree, bool enabled)
	{
		MenuFlyoutItem item = new()
		{
			Text = tree.SelectedNode?.IsStarred == true ? "Remove star" : "Add star",
			KeyboardAcceleratorTextOverride = "Ctrl+S",
			IsEnabled = enabled && tree.SelectedNode?.CanEditStar == true
		};
		item.Click += (_, _) => tree.ToggleSelectedStar();
		menu.Items.Add(item);
	}

	public static void AttachMenu(ListView list, Func<IEnumerable<Button>> commands)
	{
		list.ContextRequested += (_, args) =>
		{
			DependencyObject? source = args.OriginalSource as DependencyObject;
			while (source is not null && source != list && source is not ListViewItem) { source = VisualTreeHelper.GetParent(source); }
			if (source is ListViewItem row) { list.SelectedItem = list.ItemFromContainer(row); }
			MenuFlyout menu = new();
			foreach (Button button in commands()) { menu.Items.Add(MenuItem(button)); }
			if (args.TryGetPosition(list, out Windows.Foundation.Point point)) { menu.ShowAt(list, point); }
			else { menu.ShowAt(list); }
			args.Handled = true;
		};
	}

	private static bool Down(VirtualKey key) => (InputKeyboardSource.GetKeyStateForCurrentThread(key) & CoreVirtualKeyStates.Down) != 0;
	private static void Invoke(Button button)
	{
		if (button.IsEnabled) { new ButtonAutomationPeer(button).Invoke(); }
	}
}
