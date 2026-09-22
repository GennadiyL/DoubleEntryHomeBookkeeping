using Shared.Contracts;

namespace Shared.Impl.Services;

/// <summary>
/// Defines the date and time service.
/// Provides injectable access to current and boundary date-time values.
/// Business code consumes the shared contract instead of static DateTime members.
/// SharedDiConfiguration registers the implementation as a scoped service.
/// It does not apply business calendars or time-zone conversion rules.
/// </summary>
internal class DateTimeService : IDateTimeService
{
	public DateTime Now => DateTime.Now;

	public DateTime UtcNow => DateTime.UtcNow;

	public DateTime Today => DateTime.Today;

	// Use SQL Server compatible minimum date (1753-01-01) instead of DateTime.MinValue (0001-01-01)
	public DateTime MinValue => new(1753, 1, 1);

	public DateTime MaxValue => DateTime.MaxValue;
}
