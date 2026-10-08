using WinUi.Controls.DecimalBoxControl;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;

namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Edits a private copy of the TreeGrid column percentages.
/// Every fixed column remains visible and retains its original position.
/// Apply validates all input before calling the supplied host callback.
/// Rejected validation or saving leaves the dialog open with an error.
/// Cancel closes without changing column layout or persisted values.
/// Restore defaults changes only the pending input fields.
/// The owner supplies the XamlRoot and controls dialog lifetime.
/// Database operations and settings storage remain outside this dialog.
/// </summary>
internal sealed partial class TreeGridColumnsDialog : ContentDialog
{
	public TreeGridColumnsDialog(string[] headers, double[] widths, Func<double[], string?> apply)
	{
		Title = "Columns";
		PrimaryButtonText = "Apply";
		CloseButtonText = "Cancel";
		DefaultButton = ContentDialogButton.Primary;
		StackPanel content = new() { Spacing = 8, MinWidth = 320 };
		content.Children.Add(new TextBlock { Text = "Widths are percentages. Apply adjusts the main column to total 100%." });
		DecimalBox[] inputs = new DecimalBox[headers.Length];
		for (int i = 0; i < headers.Length; i++)
		{
			inputs[i] = new DecimalBox { Header = headers[i], Precision = 2, Value = (decimal)widths[i] };
			content.Children.Add(inputs[i]);
		}
		Button defaults = new() { Content = "Restore defaults" };
		defaults.Click += (_, _) =>
		{
			double[] values = TreeGridColumnWidths.Defaults(headers.Length);
			for (int i = 0; i < values.Length; i++)
			{
				inputs[i].Value = (decimal)values[i];
			}
		};
		content.Children.Add(defaults);
		TextBlock error = new() { TextWrapping = TextWrapping.Wrap };
		content.Children.Add(error);
		Content = new ScrollViewer { Content = content, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
		PrimaryButtonClick += (_, e) =>
		{
			double[] values = new double[inputs.Length];
			for (int i = 0; i < inputs.Length; i++)
			{
				if (!inputs[i].TryCommit() || !inputs[i].Value.HasValue)
				{
					error.Text = $"Enter a percentage for {headers[i]}.";
					inputs[i].Focus(FocusState.Programmatic);
					e.Cancel = true;
					return;
				}
				values[i] = (double)inputs[i].Value!.Value;
			}
			string? validation = TreeGridColumnWidths.AdjustMainColumn(values);
			if (values[0] is >= 0 and <= 100)
			{
				inputs[0].Value = (decimal)values[0];
			}
			else
			{
				inputs[0].Text = values[0].ToString("0.##", System.Globalization.CultureInfo.InvariantCulture);
			}
			error.Text = validation ?? apply(values) ?? string.Empty;
			e.Cancel = error.Text.Length != 0;
		};
	}
}
