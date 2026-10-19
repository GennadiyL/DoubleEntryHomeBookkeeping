using System.Runtime.InteropServices;
using Dehb.WinUi.Common;
using Microsoft.UI.Dispatching;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;

namespace Dehb.WinUi.Dialogs;

/// <summary>
/// Provides hover hints within an owned editor window.
/// Uses the existing ToolTipService content rather than separate hint strings.
/// Resolves command buttons through the hovered visual ancestors.
/// Renders text hints in a noninteractive overlay inside the editor.
/// Reads existing hint content without opening another native popup.
/// A short delay avoids opening hints while the pointer crosses controls.
/// Hints close on input, window deactivation, unloading, or pointer exit.
/// Dynamic entry rows participate without per-row event subscriptions.
/// This behavior neither activates windows nor changes keyboard focus.
/// </summary>
internal static partial class EditorToolTips
{
	private static int _traceLines;
	private static readonly string TracePath = Path.Combine(AppContext.BaseDirectory, "Dehb-popup-hints.log");

	private static void Trace(string message)
	{
		if (_traceLines >= 300) { return; }
		try
		{
			string line = $"{DateTimeOffset.Now:O} {message}{Environment.NewLine}";
			if (_traceLines++ == 0) { File.WriteAllText(TracePath, line); }
			else { File.AppendAllText(TracePath, line); }
		}
		catch (IOException) { }
		catch (UnauthorizedAccessException) { }
	}

	private static readonly DependencyProperty HintProperty = DependencyProperty.RegisterAttached(
		"Hint", typeof(string), typeof(EditorToolTips), new PropertyMetadata(null));

	public static void SetHint(FrameworkElement control, string hint)
	{
		if (!Equals(control.GetValue(HintProperty), hint)) { control.SetValue(HintProperty, hint); }
		if (ToolTipService.GetToolTip(control) is not null) { ToolTipService.SetToolTip(control, null); }
		if (control.KeyboardAcceleratorPlacementMode != KeyboardAcceleratorPlacementMode.Hidden)
		{
			control.KeyboardAcceleratorPlacementMode = KeyboardAcceleratorPlacementMode.Hidden;
		}
	}

	private static object GetHint(FrameworkElement control) => ToolTipService.GetToolTip(control) ?? control.GetValue(HintProperty);

	public static void Attach(Window window, Grid root, Func<bool> isActive)
	{
		DispatcherQueueTimer timer = root.DispatcherQueue.CreateTimer();
		timer.Interval = TimeSpan.FromMilliseconds(600);
		timer.IsRepeating = false;
		FrameworkElement? target = null;
		Canvas layer = new() { IsHitTestVisible = false };
		TextBlock text = new() { FontSize = Ui.FontSize, TextWrapping = TextWrapping.Wrap };
		Border hint = new()
		{
			Child = text,
			Padding = new Thickness(8, 4, 8, 4),
			BorderThickness = new Thickness(1),
			CornerRadius = new CornerRadius(4),
			Visibility = Visibility.Collapsed
		};
		layer.Children.Add(hint);
		Grid.SetRowSpan(layer, Math.Max(1, root.RowDefinitions.Count));
		Grid.SetColumnSpan(layer, Math.Max(1, root.ColumnDefinitions.Count));
		root.Children.Add(layer);
		Windows.Foundation.Point pointer = default;
		IntPtr handle = WinRT.Interop.WindowNative.GetWindowHandle(window);

		bool CanShow() => GetForegroundWindow() == handle && isActive();
		Trace($"Attach window={window.Title} handle={handle}");

		void Close()
		{
			timer.Stop();
			hint.Visibility = Visibility.Collapsed;
			text.Text = string.Empty;
			target = null;
		}

		timer.Tick += (_, _) =>
		{
			Trace($"Timer window={window.Title} target={target?.GetType().Name} loaded={target?.IsLoaded} foreground={GetForegroundWindow()} handle={handle} top={isActive()} layer={layer.ActualWidth}x{layer.ActualHeight}");
			if (target is not { IsLoaded: true, XamlRoot: not null } || !CanShow() ||
				VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Any(popup => popup.Child is not ToolTip && (popup.IsLightDismissEnabled || popup.Child is ContentDialog)))
			{
				Trace($"Blocked window={window.Title} popups={string.Join(",", root.XamlRoot is null ? [] : VisualTreeHelper.GetOpenPopupsForXamlRoot(root.XamlRoot).Select(popup => $"{popup.Child?.GetType().Name}:{popup.IsLightDismissEnabled}"))}");
				Close();
				return;
			}
			object content = GetHint(target);
			if (content is null || content is string { Length: 0 }) { return; }
			string? label = (content is ToolTip configured ? configured.Content : content) as string;
			if (string.IsNullOrEmpty(label) || layer.ActualWidth <= 0 || layer.ActualHeight <= 0)
			{
				Trace($"No hint window={window.Title} contentType={content?.GetType().Name} label={label}");
				return;
			}
			bool dark = root.ActualTheme == ElementTheme.Dark;
			text.Foreground = new SolidColorBrush(dark ? Microsoft.UI.Colors.White : Microsoft.UI.Colors.Black);
			hint.Background = new SolidColorBrush(dark ? Microsoft.UI.Colors.Black : Microsoft.UI.Colors.White);
			hint.BorderBrush = new SolidColorBrush(Microsoft.UI.Colors.Gray);
			text.Text = label;
			hint.MaxWidth = Math.Min(480, layer.ActualWidth);
			hint.MaxHeight = layer.ActualHeight;
			hint.Visibility = Visibility.Visible;
			hint.Measure(new Windows.Foundation.Size(hint.MaxWidth, hint.MaxHeight));
			double x = Math.Clamp(pointer.X, 0, Math.Max(0, layer.ActualWidth - hint.DesiredSize.Width));
			double y = pointer.Y + 20;
			if (y + hint.DesiredSize.Height > layer.ActualHeight) { y = pointer.Y - hint.DesiredSize.Height - 8; }
			Canvas.SetLeft(hint, x);
			Canvas.SetTop(hint, Math.Max(0, y));
			Trace($"Show window={window.Title} label={label} position={x},{y} size={hint.DesiredSize.Width}x{hint.DesiredSize.Height}");
		};

		root.AddHandler(UIElement.PointerMovedEvent, new PointerEventHandler((_, args) =>
		{
			pointer = args.GetCurrentPoint(layer).Position;
			FrameworkElement? candidate = null;
			DependencyObject? source = args.OriginalSource as DependencyObject;
			while (source is not null)
			{
				if (source is ButtonBase element && element is not CheckBox && GetHint(element) is { } content &&
					content is not string { Length: 0 })
				{
					string? label = (content is ToolTip configured ? configured.Content : content) as string;
					if (!string.IsNullOrEmpty(label))
					{
						SetHint(element, label);
						candidate = element;
					}
					break;
				}
				if (source == root) { break; }
				source = VisualTreeHelper.GetParent(source);
			}
			if (ReferenceEquals(candidate, target))
			{
				if (candidate is not null && hint.Visibility != Visibility.Visible && !timer.IsRunning && CanShow()) { timer.Start(); }
				return;
			}
			Trace($"Hover window={window.Title} source={args.OriginalSource?.GetType().Name} button={candidate?.GetType().Name} hint={(candidate is null ? null : GetHint(candidate))} canShow={CanShow()}");
			Close();
			if (candidate is not null && CanShow())
			{
				target = candidate;
				timer.Start();
			}
		}), true);
		root.PointerExited += (_, _) =>
		{
			if (!GetCursorPos(out NativePoint position)) { return; }
			IntPtr pointerWindow = GetAncestor(WindowFromPoint(position), 2);
			Trace($"PointerExit window={window.Title} pointerWindow={pointerWindow} handle={handle} dismiss={pointerWindow != handle}");
			if (pointerWindow != handle) { Close(); }
		};
		root.AddHandler(UIElement.PointerPressedEvent, new PointerEventHandler((_, _) => Close()), true);
		root.PreviewKeyDown += (_, _) => Close();
		root.Unloaded += (_, _) => Close();
		window.Activated += (_, args) =>
		{
			Trace($"Activation window={window.Title} state={args.WindowActivationState}");
			if (args.WindowActivationState == WindowActivationState.Deactivated) { Close(); }
		};
		window.Closed += (_, _) => Close();
	}

	[StructLayout(LayoutKind.Sequential)]
	private struct NativePoint
	{
		internal int X;
		internal int Y;
	}

	[LibraryImport("user32.dll")]
	private static partial IntPtr GetForegroundWindow();

	[LibraryImport("user32.dll")]
	[return: MarshalAs(UnmanagedType.Bool)]
	private static partial bool GetCursorPos(out NativePoint point);

	[LibraryImport("user32.dll")]
	private static partial IntPtr WindowFromPoint(NativePoint point);

	[LibraryImport("user32.dll")]
	private static partial IntPtr GetAncestor(IntPtr handle, uint flags);
}
