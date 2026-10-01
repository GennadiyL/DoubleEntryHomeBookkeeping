using System.Globalization;
#pragma warning disable CA1305

namespace Business.Contracts.Utils.Convertors;

/// <summary>
/// Converts timestamps to and from the application timestamp text format.
/// Formatting uses a fixed year-month-day and time layout with milliseconds.
/// The overload without an input formats the current UTC time.
/// Parsing uses the invariant culture and requires the exact configured format.
/// The text contains no timezone suffix or offset.
/// Callers retain responsibility for the UTC timestamp convention.
/// This helper does not calculate device-local calendar boundaries.
/// Conversion does not read or persist business entities.
/// </summary>
public static class DateTimeConvertor
{
	public const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff";

	public static string ConvertToString(DateTime dateTime) => dateTime.ToString(DateTimeFormat);

	public static string ConvertToString() => ConvertToString(DateTime.UtcNow);

	public static DateTime ConvertFromString(string stamp) => DateTime.ParseExact(stamp, DateTimeFormat, CultureInfo.InvariantCulture);
}
