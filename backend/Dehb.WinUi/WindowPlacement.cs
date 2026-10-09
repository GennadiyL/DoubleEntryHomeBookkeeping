using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using System.Text.Json;
using Windows.Graphics;

namespace Dehb.WinUi;

internal sealed class WindowPlacement
{
	public int X { get; set; }
	public int Y { get; set; }
	public int Width { get; set; } = 1280;
	public int Height { get; set; } = 820;
	public bool Maximized { get; set; }
	private static string FilePath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "DoubleEntryHomeBookkeeping", "window.json");
	public static void Attach(Window window)
	{
		WindowPlacement state = new();
		try
		{ if (File.Exists(FilePath))
			{
				state = JsonSerializer.Deserialize<WindowPlacement>(File.ReadAllText(FilePath)) ?? state;
			}
		}
		catch (IOException) { }
		catch (JsonException) { }
		RectInt32 rectangle = new(state.X, state.Y, Math.Max(1100, state.Width), Math.Max(700, state.Height));
		DisplayArea display = DisplayArea.GetFromRect(rectangle, DisplayAreaFallback.Nearest);
		rectangle.X = Math.Clamp(rectangle.X, display.WorkArea.X, Math.Max(display.WorkArea.X, display.WorkArea.X + display.WorkArea.Width - rectangle.Width));
		rectangle.Y = Math.Clamp(rectangle.Y, display.WorkArea.Y, Math.Max(display.WorkArea.Y, display.WorkArea.Y + display.WorkArea.Height - rectangle.Height));
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
			if (size.Width < 1100 || size.Height < 700)
			{ window.AppWindow.Resize(new SizeInt32(Math.Max(1100, size.Width), Math.Max(700, size.Height))); return; }
			state.X = window.AppWindow.Position.X;
			state.Y = window.AppWindow.Position.Y;
			state.Width = size.Width;
			state.Height = size.Height;
		};
		window.Closed += (_, _) =>
		{
			try
			{ Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!); File.WriteAllText(FilePath, JsonSerializer.Serialize(state)); }
			catch (IOException) { }
		};
	}
}
