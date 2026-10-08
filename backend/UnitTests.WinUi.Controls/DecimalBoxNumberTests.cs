using System.Globalization;
using NUnit.Framework;
using WinUi.Controls.DecimalBoxControl;

namespace UnitTests.WinUi.Controls;

/// <summary>
/// Tests decimal parsing without creating a WinUI dispatcher.
/// Plain input uses a dot regardless of the current display culture.
/// Commit rounds using midpoint-to-even at the specified precision.
/// Empty input remains optional and incomplete numbers fail at commit.
/// Currency, grouping, expressions and exponents are rejected.
/// Decimal overflow and silently rounded source values are rejected.
/// Large exactly representable amounts retain all their significant digits.
/// Regional display formatting remains separate from invariant editing.
/// </summary>
[TestFixture(Category = "Local")]
public sealed class DecimalBoxNumberTests
{
	[TestCase(" 12.345 ", "12.34")]
	[TestCase("12.355", "12.36")]
	[TestCase("-12.355", "-12.36")]
	[TestCase(".5", "0.50")]
	[TestCase("12.", "12.00")]
	public void Commit_RoundsAtRequestedPrecision(string input, string expected)
	{
		Assert.That(DecimalBoxNumber.TryParse(input, 2, out decimal? value), Is.True);
		Assert.That(value, Is.EqualTo(decimal.Parse(expected, CultureInfo.InvariantCulture)));
	}

	[TestCase("-")]
	[TestCase(".")]
	[TestCase("1,000.00")]
	[TestCase("12,34")]
	[TestCase("$12")]
	[TestCase("1e2")]
	[TestCase("1+2")]
	[TestCase("79228162514264337593543950336")]
	[TestCase("0.12345678901234567890123456789")]
	public void InvalidOrInexactInput_IsRejected(string input)
	{
		Assert.That(DecimalBoxNumber.TryParse(input, 2, out _), Is.False);
	}

	[TestCase(0, "1")]
	[TestCase(2, "0.01")]
	[TestCase(4, "0.0001")]
	[TestCase(28, "0.0000000000000000000000000001")]
	public void SpinStep_FollowsPrecision(int precision, string expected)
	{
		Assert.That(DecimalBoxNumber.Step(precision), Is.EqualTo(decimal.Parse(expected, CultureInfo.InvariantCulture)));
	}

	[Test]
	public void Spin_IncreasesAndDecreasesWithoutFloatingPointDrift()
	{
		Assert.That(DecimalBoxNumber.TryStep(10m, 4, true, out decimal next), Is.True);
		Assert.That(next, Is.EqualTo(10.0001m));
		Assert.That(DecimalBoxNumber.TryStep(next, 4, false, out decimal original), Is.True);
		Assert.That(original, Is.EqualTo(10m));
		Assert.That(DecimalBoxNumber.TryStep(0m, 2, false, out decimal negative), Is.True);
		Assert.That(negative, Is.EqualTo(-0.01m));
	}

	[Test]
	public void Spin_OverflowAndUnrepresentableStep_KeepOriginalValue()
	{
		Assert.That(DecimalBoxNumber.TryStep(decimal.MaxValue, 0, true, out decimal overflow), Is.False);
		Assert.That(overflow, Is.EqualTo(decimal.MaxValue));
		Assert.That(DecimalBoxNumber.TryStep(decimal.MaxValue, 2, false, out decimal inexact), Is.False);
		Assert.That(inexact, Is.EqualTo(decimal.MaxValue));
		Assert.That(DecimalBoxNumber.TryStep(decimal.MinValue, 0, false, out decimal underflow), Is.False);
		Assert.That(underflow, Is.EqualTo(decimal.MinValue));
	}

	[TestCase("0123456789.+-", true)]
	[TestCase("", true)]
	[TestCase("-12.5", true)]
	[TestCase("1,2", false)]
	[TestCase("12e3", false)]
	[TestCase(" 12", false)]
	[TestCase("$12", false)]
	[TestCase("１２", false)]
	public void Input_AllowsOnlyRequestedCharacters(string text, bool allowed)
	{
		Assert.That(DecimalBoxNumber.IsAllowedInput(text), Is.EqualTo(allowed));
	}

	[Test]
	public void EmptyInput_IsNotZero()
	{
		Assert.That(DecimalBoxNumber.TryParse("  ", 2, out decimal? value), Is.True);
		Assert.That(value, Is.Null);
	}

	[Test]
	public void LargeAmount_PreservesDecimalDigits()
	{
		Assert.That(DecimalBoxNumber.TryParse("9007199254740993.25", 2, out decimal? value), Is.True);
		Assert.That(value, Is.EqualTo(9007199254740993.25m));
	}

	[Test]
	public void PrecisionZeroAndTwentyEight_AreSupported()
	{
		Assert.That(DecimalBoxNumber.TryParse("2.5", 0, out decimal? integer), Is.True);
		Assert.That(integer, Is.EqualTo(2m));
		Assert.That(DecimalBoxNumber.TryParse("0.0000000000000000000000000001", 28, out decimal? small), Is.True);
		Assert.That(small, Is.EqualTo(0.0000000000000000000000000001m));
	}

	[Test]
	public void EditingUsesDot_WhileDisplayUsesRegionalSettings()
	{
		CultureInfo previous = CultureInfo.CurrentCulture;
		try
		{
			CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
			Assert.That(DecimalBoxNumber.EditText(1234.56m), Is.EqualTo("1234.56"));
			Assert.That(DecimalBoxNumber.DisplayText(1234.56m, 2), Is.EqualTo("1.234,56"));
			Assert.That(DecimalBoxNumber.TryParse("1234.56", 2, out decimal? value), Is.True);
			Assert.That(value, Is.EqualTo(1234.56m));
		}
		finally
		{
			CultureInfo.CurrentCulture = previous;
		}
	}
}
