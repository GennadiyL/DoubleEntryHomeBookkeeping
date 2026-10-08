using NUnit.Framework;
using WinUi.Controls.TreeGridControl;

namespace UnitTests.WinUi.Controls;

/// <summary>
/// Validates percentage layouts without creating UI controls.
/// Default layouts reserve room for hierarchy and star controls.
/// Every accepted layout totals exactly one hundred percent.
/// Invalid counts and nonfinite values are rejected before layout changes.
/// Narrow columns fail independently of the total percentage.
/// Two decimal places match the editable and persisted representation.
/// Validation never mutates the supplied settings or default values.
/// These cases run entirely in memory on the local machine.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class TreeGridColumnWidthsTests
{
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(6)]
	public void Defaults_TotalOneHundredAndPreserveMinimums(int count)
	{
		double[] widths = TreeGridColumnWidths.Defaults(count);
		Assert.That(TreeGridColumnWidths.Validate(widths, count), Is.Null);
		Assert.That(widths.Sum(), Is.EqualTo(100).Within(0.000001));
		for (int i = 0; i < count; i++)
		{
			Assert.That(widths[i] / 100 * TreeGridColumnWidths.MinimumRowWidth(count),
				Is.GreaterThanOrEqualTo(TreeGridColumnWidths.MinimumPixels(i, count)));
		}
	}

	[TestCase(70, 20, 10)]
	[TestCase(60, 30, 10)]
	public void ValidPercentages_Accepted(double name, double data, double star)
	{
		Assert.That(TreeGridColumnWidths.Validate(new[] { name, data, star }, 3), Is.Null);
	}

	[TestCase(70, 20, 9)]
	[TestCase(80, 19, 1)]
	[TestCase(50, 40, 10)]
	[TestCase(70, 0, 30)]
	[TestCase(double.NaN, 20, 10)]
	[TestCase(double.PositiveInfinity, 20, 10)]
	[TestCase(70.001, 19.999, 10)]
	public void InvalidPercentages_Rejected(double name, double data, double star)
	{
		Assert.That(TreeGridColumnWidths.Validate(new[] { name, data, star }, 3), Is.Not.Null);
	}

	[TestCase(70, 10, 10, 80, true)]
	[TestCase(90, 20, 10, 70, true)]
	[TestCase(70, 20, 10, 70, true)]
	[TestCase(70, 60, 10, 30, false)]
	[TestCase(70, -20, 10, 110, false)]
	public void AdjustMainColumn_BalancesOnlyMainAndChecksLimits(double main, double data, double star, double expected, bool valid)
	{
		double[] widths = { main, data, star };
		string? error = TreeGridColumnWidths.AdjustMainColumn(widths);
		Assert.That(widths[0], Is.EqualTo(expected));
		Assert.That(widths[1], Is.EqualTo(data));
		Assert.That(widths[2], Is.EqualTo(star));
		Assert.That(error is null, Is.EqualTo(valid));
	}

	[Test]
	public void SettingsForDifferentColumnCount_Rejected()
	{
		double[] savedWidths = { 90, 10 };
		Assert.That(TreeGridColumnWidths.Validate(savedWidths, 3), Is.Not.Null);
	}
}
