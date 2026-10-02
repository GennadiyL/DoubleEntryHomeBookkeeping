namespace Business.Models.Constants;

/// <summary>
/// Defines shared date values for bookkeeping models and validation services.
/// InitialDate identifies the hidden fallback currency-rate date.
/// MinDate and MaxDate expose calendar-date boundaries to consuming operations.
/// MinDateTime provides the minimum transaction instant with an explicit UTC kind.
/// These shared values are application constants rather than editable configuration.
/// Consumers apply the values; this class does not create entities or enforce validation.
/// </summary>
public static class AppValues
{
	public static readonly DateOnly InitialDate = new(1970, 1, 1);
	public static readonly DateOnly MinDate = new(2001, 1, 1);
	public static readonly DateOnly MaxDate = new(2100, 1, 1);
	public static readonly DateTime MinDateTime = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
	public const decimal MinDecimal = long.MinValue / 10000m;
	public const decimal MaxDecimal = long.MaxValue / 10000m;

}
