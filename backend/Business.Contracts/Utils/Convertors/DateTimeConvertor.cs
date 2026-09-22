using System.Globalization;
#pragma warning disable CA1305

namespace Business.Contracts.Utils.Convertors;

public static class DateTimeConvertor
{
	public const string DateTimeFormat = "yyyy-MM-dd'T'HH:mm:ss.fff";

	public static string ConvertToString(DateTime dateTime) => dateTime.ToString(DateTimeFormat);

	public static string ConvertToString() => ConvertToString(DateTime.UtcNow);

	public static DateTime ConvertFromString(string stamp) => DateTime.ParseExact(stamp, DateTimeFormat, CultureInfo.InvariantCulture);
}
