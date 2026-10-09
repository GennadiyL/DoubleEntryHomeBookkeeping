using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using System.Runtime.InteropServices;
using Windows.Graphics;

namespace Dehb.WinUi;

internal sealed partial class EditorWindow : Window
{
	private readonly Window _owner;
	private readonly PopupCoordinator _coordinator;
	private readonly TaskCompletionSource<bool> _closed = new();
	private bool _closing;
	private bool _checking;
	private bool _saved;
	public StackPanel Fields { get; } = new() { Spacing = 12 };
	public TextBlock Error { get; } = new() { TextWrapping = TextWrapping.Wrap };
	public StackPanel Commands { get; } = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
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
		Grid grid = new() { Padding = new Thickness(fixedSize.HasValue ? 16 : 20), RowSpacing = 12 };
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
		Grid.SetRow(Commands, 2);
		grid.Children.Add(Commands);
		Content = grid;
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
	public void Finish(bool saved) { _saved = saved; _closing = true; Close(); }
	public void SaveButton(Action save)
	{
		Commands.Children.Add(Ui.Button("Save", () => { save(); Finish(true); }, Error));
		Commands.Children.Add(Ui.Button("Cancel", Close, Error));
	}
	[LibraryImport("user32.dll")]
	private static partial uint GetDpiForWindow(IntPtr handle);

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool EnableWindow(IntPtr handle, [MarshalAs(UnmanagedType.Bool)] bool enable);
}
