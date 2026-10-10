using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Controls;
using WinUi.Controls.DecimalBoxControl;

namespace Dehb.WinUi.Common;

internal static class Ui
{
	public static double FontSize => ((App)Application.Current).UiSettings.Appearance.FontSize;
	public static double RowHeight => ((App)Application.Current).UiSettings.Appearance.RowHeight;

	public static LinearGradientBrush Background => new()
	{
		StartPoint = new Windows.Foundation.Point(0, 0),
		EndPoint = new Windows.Foundation.Point(1, 1),
		GradientStops =
		{
			new GradientStop { Offset = 0, Color = ParseColor(((App)Application.Current).UiSettings.Appearance.BackgroundStartColor) },
			new GradientStop { Offset = 1, Color = ParseColor(((App)Application.Current).UiSettings.Appearance.BackgroundEndColor) }
		}
	};
	public static SolidColorBrush SelectionBorder => new(ParseColor(((App)Application.Current).UiSettings.Appearance.SelectionBorderColor));
	public static Windows.UI.Color ParseColor(string value) => Windows.UI.Color.FromArgb(
		255, Convert.ToByte(value.Substring(1, 2), 16), Convert.ToByte(value.Substring(3, 2), 16), Convert.ToByte(value.Substring(5, 2), 16));

	public static void ApplyFontSettings()
	{
		ResourceDictionary resources = Application.Current.Resources;
		foreach (string key in new[] { "ControlContentThemeFontSize", "TextControlThemeFontSize", "ContentDialogTitleFontSize" })
		{
			resources[key] = FontSize;
		}
		Style textStyle = new(typeof(TextBlock));
		textStyle.Setters.Add(new Setter(TextBlock.FontSizeProperty, FontSize));
		resources[typeof(TextBlock)] = textStyle;
		foreach (Type type in new[] { typeof(Button), typeof(TextBox), typeof(ComboBox), typeof(ComboBoxItem), typeof(CheckBox),
			typeof(CalendarDatePicker), typeof(DatePicker), typeof(ListView), typeof(ListViewItem), typeof(ContentDialog),
			typeof(MenuFlyoutItem), typeof(ToolTip), typeof(DecimalBox) })
		{
			Style style = new(type);
			style.Setters.Add(new Setter(Control.FontSizeProperty, FontSize));
			resources[type] = style;
		}
	}

	public static Button Button(string label, Action action, TextBlock error)
	{
		Button button = new() { Content = label };
		button.Click += (_, _) => { try { error.Text = ""; action(); } catch (Exception e) { error.Text = e.Message; } };
		return button;
	}
	public static Button AsyncButton(string label, Func<Task> action, TextBlock error)
	{
		Button button = new() { Content = label };
		button.Click += async (_, _) => { try { error.Text = ""; await action(); } catch (Exception e) { error.Text = e.Message; } };
		return button;
	}
	public static StackPanel Row(params UIElement[] elements)
	{
		StackPanel panel = new() { Orientation = Orientation.Horizontal, Spacing = 8 };
		foreach (UIElement element in elements)
		{
			panel.Children.Add(element);
		}

		return panel;
	}
	public static decimal Number(DecimalBox box)
	{
		if (!box.TryCommit() || box.Value is null)
		{
			throw new InvalidOperationException("Enter a valid number.");
		}

		return box.Value.Value;
	}
	public static async Task<bool> Confirm(Window window, string message)
	{
		ContentDialog dialog = new()
		{
			XamlRoot = ((FrameworkElement)window.Content).XamlRoot,
			Title = message,
			Background = Background,
			PrimaryButtonText = "Yes",
			CloseButtonText = "No",
			DefaultButton = ContentDialogButton.Close
		};
		return await dialog.ShowAsync() == ContentDialogResult.Primary;
	}
	public static void StretchRows(ListView list)
	{
		Style style = new(typeof(ListViewItem));
		style.Setters.Add(new Setter(Control.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch));
		style.Setters.Add(new Setter(FrameworkElement.HeightProperty, RowHeight));
		style.Setters.Add(new Setter(FrameworkElement.MinHeightProperty, RowHeight));
		style.Setters.Add(new Setter(Control.PaddingProperty, new Thickness(8, 0, 8, 0)));
		style.Setters.Add(new Setter(Control.VerticalContentAlignmentProperty, VerticalAlignment.Center));
		style.Setters.Add(new Setter(Control.FocusVisualPrimaryBrushProperty, SelectionBorder));
		list.ItemContainerStyle = style;
		ScrollViewer.SetHorizontalScrollMode(list, ScrollMode.Disabled);
		ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
	}
	public static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
}
