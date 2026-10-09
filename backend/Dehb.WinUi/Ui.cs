using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using WinUi.Controls.DecimalBoxControl;

namespace Dehb.WinUi;

internal static class Ui
{
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
		list.ItemContainerStyle = style;
		ScrollViewer.SetHorizontalScrollMode(list, ScrollMode.Disabled);
		ScrollViewer.SetHorizontalScrollBarVisibility(list, ScrollBarVisibility.Disabled);
	}
	public static TextBlock Text(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
}
