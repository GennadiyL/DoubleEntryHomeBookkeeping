using Dehb.WinUi.UiSettingsTypes;
using System.Runtime.InteropServices;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Text.Json;
using Windows.Graphics;

namespace Dehb.WinUi.WindowManagement;

internal sealed partial class WindowPlacement
{
	private WindowSubclass? _subclass;
	public int X { get; set; }
	public int Y { get; set; }
	public int Width { get; set; } = 1280;
	public int Height { get; set; } = 820;
	public bool Maximized { get; set; }
	private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoubleEntryHomeBookkeeping", "window.json");
	public static void Attach(Window window, UiMainWindowSettings settings)
	{
		SizeInt32 ConfiguredSize()
		{
			double scale = GetDpiForWindow(WinRT.Interop.WindowNative.GetWindowHandle(window)) / 96d;
			return new SizeInt32((int)Math.Ceiling(settings.Width * scale), (int)Math.Ceiling(settings.MinHeight * scale));
		}
		SizeInt32 minimum = ConfiguredSize();
		WindowPlacement state = new();
		try
		{ if (File.Exists(FilePath))
			{
				state = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(FilePath)) ?? state;
			}
		}
		catch (IOException) { }
		catch (JsonException) { }
		RectInt32 rectangle = new(state.X, state.Y, minimum.Width, Math.Max(minimum.Height, state.Height));
		DisplayArea display = DisplayArea.GetFromRect(rectangle, DisplayAreaFallback.Nearest);
		rectangle.X = Math.Clamp(rectangle.X, display.WorkArea.X, Math.Max(display.WorkArea.X, display.WorkArea.X + display.WorkArea.Width - rectangle.Width));
		rectangle.Y = Math.Clamp(rectangle.Y, display.WorkArea.Y, Math.Max(display.WorkArea.Y, display.WorkArea.Y + display.WorkArea.Height - rectangle.Height));
		IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(window);
		state._subclass = (hwnd, message, wParam, lParam, id, data) =>
		{
			IntPtr result = DefSubclassProc(hwnd, message, wParam, lParam);
			if (message == 0x0024)
			{
				SizeInt32 limits = ConfiguredSize();
				MinMaxInfo info = Marshal.PtrToStructure<MinMaxInfo>(lParam);
				info.MinTrackSize.X = limits.Width;
				info.MaxTrackSize.X = limits.Width;
				info.MinTrackSize.Y = limits.Height;
				info.MaxSize.X = limits.Width;
				DisplayArea area = DisplayArea.GetFromWindowId(window.AppWindow.Id, DisplayAreaFallback.Nearest);
				info.MaxSize.Y = area.WorkArea.Height;
				info.MaxPosition.Y = area.WorkArea.Y - area.OuterBounds.Y;
				info.MaxPosition.X = Math.Clamp(state.X, area.WorkArea.X,
					Math.Max(area.WorkArea.X, area.WorkArea.X + area.WorkArea.Width - limits.Width)) - area.OuterBounds.X;
				Marshal.StructureToPtr(info, lParam, false);
				return IntPtr.Zero;
			}
			return result;
		};
		IntPtr callback = Marshal.GetFunctionPointerForDelegate(state._subclass);
		if (!SetWindowSubclass(handle, callback, 1, 0))
		{
			throw new InvalidOperationException("Cannot apply the main-window width constraint.");
		}
		window.AppWindow.MoveAndResize(rectangle);
		if (state.Maximized && window.AppWindow.Presenter is OverlappedPresenter presenter)
		{
			presenter.Maximize();
		}

		window.AppWindow.Changed += (_, _) =>
		{
			if (window.AppWindow.Presenter is not OverlappedPresenter current)
			{
				return;
			}

			state.Maximized = current.State == OverlappedPresenterState.Maximized;
			if (current.State != OverlappedPresenterState.Restored)
			{
				return;
			}

			SizeInt32 size = window.AppWindow.Size;
			minimum = ConfiguredSize();
			if (size.Width != minimum.Width || size.Height < minimum.Height)
			{ window.AppWindow.Resize(new SizeInt32(minimum.Width, Math.Max(minimum.Height, size.Height))); return; }
			state.X = window.AppWindow.Position.X;
			state.Y = window.AppWindow.Position.Y;
			state.Width = size.Width;
			state.Height = size.Height;
		};
		window.Closed += (_, _) =>
		{
			RemoveWindowSubclass(handle, callback, 1);
			GC.KeepAlive(state._subclass);
			try
			{ Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); File.WriteAllText(FilePath, JsonSerializer.Serialize(state)); }
			catch (IOException) { }
		};
	}
	[StructLayout(LayoutKind.Sequential)]
	private struct MinMaxInfo
	{
		public PointInt32 Reserved;
		public PointInt32 MaxSize;
		public PointInt32 MaxPosition;
		public PointInt32 MinTrackSize;
		public PointInt32 MaxTrackSize;
	}

	[UnmanagedFunctionPointer(CallingConvention.Winapi)]
	private delegate IntPtr WindowSubclass(IntPtr window, uint message, nuint wParam, IntPtr lParam, nuint id, nuint data);

	[LibraryImport("comctl32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool SetWindowSubclass(IntPtr window, IntPtr callback, nuint id, nuint data);

	[LibraryImport("comctl32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool RemoveWindowSubclass(IntPtr window, IntPtr callback, nuint id);

	[LibraryImport("comctl32.dll")]
	private static partial IntPtr DefSubclassProc(IntPtr window, uint message, nuint wParam, IntPtr lParam);

	[LibraryImport("user32.dll")]
	private static partial uint GetDpiForWindow(IntPtr handle);
}
