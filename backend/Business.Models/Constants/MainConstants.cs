namespace Business.Models.Constants;

public static class MainConstants
{
	public static readonly DateOnly InitialDate = new(1970, 1, 1);
	public static readonly DateOnly MinDate = new(2001, 1, 1);
	public static readonly DateOnly MaxDate = new(2100, 1, 1);
	public static readonly DateTime MinDateTime = new(2001, 1, 1, 0, 0, 0, DateTimeKind.Utc);
}
