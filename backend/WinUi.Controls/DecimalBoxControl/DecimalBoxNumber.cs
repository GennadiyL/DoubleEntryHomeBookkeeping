using System.Globalization;
using System.Text.RegularExpressions;

namespace WinUi.Controls.DecimalBoxControl;

/// <summary>
/// Parses plain dot-decimal text without a floating-point conversion.
/// Empty text represents an optional value rather than an implicit zero.
/// Surrounding whitespace is accepted while grouping and symbols are rejected.
/// Scientific notation and arithmetic expressions are not numeric input.
/// Values outside exact decimal representation fail without silent truncation.
/// Valid values round at commit using the requested fractional precision.
/// Midpoint rounding follows the application's midpoint-to-even convention.
/// Formatting separates invariant editing text from regional display text.
/// </summary>
internal static partial class DecimalBoxNumber
{
	[GeneratedRegex(@"^[+-]?(?:[0-9]+(?:\.[0-9]*)?|\.[0-9]+)$")]
	private static partial Regex PlainNumber();

	public static bool IsAllowedInput(string text) => text.All(character => character is >= '0' and <= '9' or '.' or '-' or '+');

	public static bool TryParse(string text, int precision, out decimal? value)
	{
		value = null;
		string input = text.Trim();
		if (input.Length == 0)
		{
			return true;
		}
		if (!PlainNumber().IsMatch(input)
			|| !decimal.TryParse(input, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal number))
		{
			return false;
		}
		string[] parts = input.TrimStart('+', '-').Split('.');
		string integer = parts[0].TrimStart('0');
		string fraction = parts.Length == 2 ? parts[1].TrimEnd('0') : string.Empty;
		string canonical = (integer.Length == 0 ? "0" : integer) + (fraction.Length == 0 ? string.Empty : "." + fraction);
		if (canonical != decimal.Abs(number).ToString("0.############################", CultureInfo.InvariantCulture))
		{
			return false;
		}
		value = decimal.Round(number, precision, MidpointRounding.ToEven);
		return true;
	}

	public static decimal Step(int precision)
	{
		decimal step = 1;
		for (int i = 0; i < precision; i++)
		{
			step /= 10;
		}
		return step;
	}

	public static bool TryStep(decimal value, int precision, bool increase, out decimal result)
	{
		decimal delta = increase ? Step(precision) : -Step(precision);
		result = value;
		try
		{
			decimal next = checked(value + delta);
			if (next - value != delta) return false;
			result = next;
			return true;
		}
		catch (OverflowException)
		{
			return false;
		}
	}

	public static string EditText(decimal? value) => value?.ToString("0.############################", CultureInfo.InvariantCulture) ?? string.Empty;
	public static string DisplayText(decimal? value, int precision) => value?.ToString($"N{precision}", CultureInfo.CurrentCulture) ?? string.Empty;
}
