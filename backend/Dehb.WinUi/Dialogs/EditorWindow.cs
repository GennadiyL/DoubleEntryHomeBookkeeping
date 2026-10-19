using Dehb.WinUi.Common;
using Dehb.WinUi.WindowManagement;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Automation;
using Windows.System;
using Microsoft.UI.Input;
using Windows.UI.Core;
using System.Runtime.InteropServices;
using System.ComponentModel;
using Windows.Graphics;

namespace Dehb.WinUi.Dialogs;

internal sealed partial class EditorWindow : Window
{
	private readonly Window _owner;
	private readonly PopupCoordinator _coordinator;
	private readonly TaskCompletionSource<bool> _closed = new();
	private bool _closing;
	private bool _checking;
	private bool _saved;
	public StackPanel Fields { get; } = new() { Spacing = 12, Padding = new Thickness(8, 4, 8, 4), Margin = new Thickness(-8, -4, -8, -4) };
	public TextBlock Error { get; } = new() { TextWrapping = TextWrapping.Wrap };
	public StackPanel Commands { get; } = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
	public StackPanel RightCommands { get; } = new() { Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right };
	public Func<bool> IsDirty { get; set; } = () => false;
	public Action? BeforeClose { get; set; }
	public EditorWindow(Window owner, PopupCoordinator coordinator, string key, string title, SizeInt32? fixedSize = null)
	{
		if (coordinator.IsActive(key))
		{
			throw new InvalidOperationException("This catalog window is already open.");
		}

		_owner = owner;
		_coordinator = coordinator;
		Title = title;
		SetOwner(owner);
		Grid grid = new() { Background = Ui.Background, Padding = fixedSize.HasValue ? new Thickness(24, 16, 24, 16) : new Thickness(20), RowSpacing = 12 };
		grid.RowDefinitions.Add(new RowDefinition());
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
		if (fixedSize.HasValue)
		{
			grid.Children.Add(Fields);
		}
		else
		{
			grid.Children.Add(new ScrollViewer { Content = Fields, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled });
		}
		Grid.SetRow(Error, 1);
		grid.Children.Add(Error);
		Grid footer = new() { ColumnSpacing = 8 };
		footer.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
		footer.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
		footer.Children.Add(Commands);
		Grid.SetColumn(RightCommands, 1);
		footer.Children.Add(RightCommands);
		Grid.SetRow(footer, 2);
		grid.Children.Add(footer);
		Content = grid;
		Ui.ApplyPickerFocus(grid);
		EditorToolTips.Attach(this, grid, () => !_closing && ReferenceEquals(_coordinator.Top(this), this));
		grid.KeyDown += (_, args) =>
		{
			if (args.Key != VirtualKey.Escape || args.Handled || !CanHandleShortcut())
			{
				return;
			}
			args.Handled = true;
			Close();
		};
		if (fixedSize is { } dialogSize)
		{
			double scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(owner)) / 96d;
			AppWindow.Resize(new SizeInt32((int)Math.Ceiling(dialogSize.Width * scale), (int)Math.Ceiling(dialogSize.Height * scale)));
		}
		else
		{
			AppWindow.Resize(new SizeInt32(1180, 740));
		}
		AppWindow.IsShownInSwitchers = false;
		if (fixedSize.HasValue && AppWindow.Presenter is OverlappedPresenter presenter)
		{
			presenter.IsResizable = false;
			presenter.IsMaximizable = false;
			presenter.IsMinimizable = false;
		}
		if (!fixedSize.HasValue)
		{
			AppWindow.Changed += (_, _) =>
			{
				SizeInt32 size = AppWindow.Size;
				if (size.Width < 1100 || size.Height < 650)
				{
					AppWindow.Resize(new SizeInt32(Math.Max(size.Width, 1100), Math.Max(size.Height, 650)));
				}
			};
		}
		AppWindow.Closing += async (_, e) =>
		{
			if (_closing)
			{
				return;
			}

			e.Cancel = true;
			if (_checking)
			{
				return;
			}

			_checking = true;
			try
			{
				if (!IsDirty() || await Ui.Confirm(this, "Discard unsaved changes?"))
				{
					Finish(false);
				}
			}
			finally { _checking = false; }
		};
		Closed += (_, _) =>
		{
			BeforeClose?.Invoke();
			_coordinator.Pop(this);
			EnableWindow(WinRT.Interop.WindowNative.GetWindowHandle(_owner), true);
			_owner.Activate();
			_closed.TrySetResult(_saved);
		};
		coordinator.Push(key, this);
	}
	public Task<bool> Show()
	{
		Window main = _owner;
		while (main is EditorWindow editor)
		{
			main = editor._owner;
		}
		PointInt32 position = main.AppWindow.Position;
		SizeInt32 mainSize = main.AppWindow.Size;
		SizeInt32 dialogSize = AppWindow.Size;
		RectInt32 workArea = DisplayArea.GetFromWindowId(main.AppWindow.Id, DisplayAreaFallback.Nearest).WorkArea;
		int x = position.X + (mainSize.Width - dialogSize.Width) / 2;
		int y = position.Y + (mainSize.Height - dialogSize.Height) / 2;
		AppWindow.Move(new PointInt32(
			Math.Clamp(x, workArea.X, Math.Max(workArea.X, workArea.X + workArea.Width - dialogSize.Width)),
			Math.Clamp(y, workArea.Y, Math.Max(workArea.Y, workArea.Y + workArea.Height - dialogSize.Height))));
		EnableWindow(WinRT.Interop.WindowNative.GetWindowHandle(_owner), false);
		Activate();
		return _closed.Task;
	}
	public void SetFieldContent(UIElement content)
	{
		Grid grid = (Grid)Content;
		grid.Children.RemoveAt(0);
		grid.Children.Insert(0, content);
	}
	public void Finish(bool saved) { _saved = saved; _closing = true; Close(); }
	public void SaveButton(Action save)
	{
		void Save() { save(); Finish(true); }
		Button saveButton = Ui.DialogButton("Save", Save, Error);
		KeyboardAccelerator shortcut = new() { Key = VirtualKey.Enter, Modifiers = VirtualKeyModifiers.Control };
		shortcut.Invoked += (_, args) =>
		{
			args.Handled = true;
			if (!saveButton.IsEnabled || !CanHandleShortcut())
			{
				return;
			}
			try
			{
				Error.Text = "";
				Save();
			}
			catch (Exception e)
			{
				Error.Text = e.Message;
			}
		};
		saveButton.KeyboardAccelerators.Add(shortcut);
		AutomationProperties.SetAcceleratorKey(saveButton, "Ctrl+Enter");
		EditorToolTips.SetHint(saveButton, "Save (Ctrl+Enter)");
		Commands.Children.Add(saveButton);
		Button cancelButton = Ui.DialogButton("Cancel", Close, Error);
		AutomationProperties.SetAcceleratorKey(cancelButton, "Esc");
		EditorToolTips.SetHint(cancelButton, "Cancel (Esc)");
		Commands.Children.Add(cancelButton);
	}
	public void AddShortcut(Control control, VirtualKey key, VirtualKeyModifiers modifiers, string hint, Action action, FrameworkElement? scope = null)
	{
		(scope ?? (FrameworkElement)Content).PreviewKeyDown += (_, args) =>
		{
			if (args.Handled || (args.Key != key && args.OriginalKey != key) || !control.IsEnabled || !CanHandleShortcut()) { return; }
			VirtualKeyModifiers pressed = VirtualKeyModifiers.None;
			foreach ((VirtualKey modifierKey, VirtualKeyModifiers modifier) in new[]
			{
				(VirtualKey.Control, VirtualKeyModifiers.Control), (VirtualKey.Shift, VirtualKeyModifiers.Shift),
				(VirtualKey.Menu, VirtualKeyModifiers.Menu), (VirtualKey.LeftWindows, VirtualKeyModifiers.Windows),
				(VirtualKey.RightWindows, VirtualKeyModifiers.Windows)
			})
			{
				if ((InputKeyboardSource.GetKeyStateForCurrentThread(modifierKey) & CoreVirtualKeyStates.Down) != 0) { pressed |= modifier; }
			}
			if (pressed != modifiers) { return; }
			args.Handled = true;
			try { Error.Text = ""; action(); }
			catch (Exception exception) { Error.Text = exception.Message; }
		};
		if (control is Microsoft.UI.Xaml.Controls.Primitives.ButtonBase and not CheckBox) { EditorToolTips.SetHint(control, hint); }
		AutomationProperties.SetAcceleratorKey(control, hint[(hint.LastIndexOf('(') + 1)..].TrimEnd(')'));
	}

	private bool CanHandleShortcut() =>
		!_closing && !_checking && ReferenceEquals(_coordinator.Top(this), this) &&
		Content is FrameworkElement { XamlRoot: not null } root &&
		!VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Any(popup => popup.IsLightDismissEnabled || popup.Child is ContentDialog);

	private void SetOwner(Window owner)
	{
		IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(this);
		IntPtr ownerHandle = WinRT.Interop.WindowNative.GetWindowHandle(owner);
		IntPtr previousOwner = IntPtr.Size == 8
			? SetWindowLongPtr(handle, -8, ownerHandle)
			: new IntPtr(SetWindowLong(handle, -8, ownerHandle.ToInt32()));
		int error = Marshal.GetLastPInvokeError();
		if (previousOwner == IntPtr.Zero && error != 0)
		{
			Close();
			throw new Win32Exception(error, "Cannot assign the editor window owner.");
		}
	}

	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongPtrW", SetLastError = true)]
	private static partial IntPtr SetWindowLongPtr(IntPtr handle, int index, IntPtr value);

	[LibraryImport("user32.dll", EntryPoint = "SetWindowLongW", SetLastError = true)]
	private static partial int SetWindowLong(IntPtr handle, int index, int value);

	[LibraryImport("user32.dll")]
	private static partial uint GetDpiForWindow(IntPtr handle);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool EnableWindow(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
