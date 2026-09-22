namespace Shared.Contracts;

public interface IDateTimeService
{
	public DateTime Now { get; }
	public DateTime UtcNow { get; }
	public DateTime Today { get; }
	public DateTime MinValue { get; }
	public DateTime MaxValue { get; }
}
