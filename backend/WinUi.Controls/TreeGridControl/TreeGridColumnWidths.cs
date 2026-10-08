namespace WinUi.Controls.TreeGridControl;

/// <summary>
/// Defines percentage sizing for name, data and star columns.
/// The complete width sequence follows the displayed column order.
/// Values must total one hundred and remain finite and positive.
/// Minimum percentages preserve hierarchy controls and readable cells.
/// A shared minimum row width makes these limits independent of resizing.
/// Default widths reserve space for every column and favor the name.
/// Validation is shared by settings restoration and the Columns dialog.
/// This helper contains no UI state or persistence behavior.
/// </summary>
internal static class TreeGridColumnWidths
{
	public static double MinimumRowWidth(int count) => Math.Max(480, 280 + (count - 2) * 60 + 36 + 120);
	public static double MinimumPixels(int index, int count) => index == 0 ? 280 : index == count - 1 ? 36 : 40;

	public static double[] Defaults(int count)
	{
		double[] widths = new double[count];
		double available = MinimumRowWidth(count);
		for (int i = 1; i < count; i++)
		{
			widths[i] = Math.Ceiling(MinimumPixels(i, count) / available * 10000) / 100;
		}
		widths[0] = Math.Round(100 - widths.Sum(), 2);
		return widths;
	}

	public static string? AdjustMainColumn(double[] widths)
	{
		if (Math.Abs(widths.Sum() - 100) > 0.000001)
		{
			widths[0] = Math.Round(100 - widths.Skip(1).Sum(), 2);
		}
		return Validate(widths, widths.Length);
	}

	public static string? Validate(IReadOnlyList<double> widths, int count)
	{
		if (widths.Count != count)
		{
			return "The number of widths must match the columns.";
		}
		if (widths.Any(width => !double.IsFinite(width) || width <= 0 || width > 100))
		{
			return "Enter a positive percentage for every column.";
		}
		if (widths.Any(width => Math.Abs(width - Math.Round(width, 2)) > 0.000001))
		{
			return "Use at most two decimal places.";
		}
		if (Math.Abs(widths.Sum() - 100) > 0.000001)
		{
			return "Column widths must total 100%.";
		}
		for (int i = 0; i < count; i++)
		{
			double minimum = Math.Ceiling(MinimumPixels(i, count) / MinimumRowWidth(count) * 10000) / 100;
			if (widths[i] < minimum)
			{
				return $"Column {i + 1} must be at least {minimum:0.##}%.";
			}
		}
		return null;
	}
}
