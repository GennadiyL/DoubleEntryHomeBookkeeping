using Shared.Contracts;

namespace Tests.Common.Mocks.Shared;

/// <summary>
/// Defines the date-time service test double.
/// Provides deterministic current and boundary date-time values for tests.
/// Tests construct or derive from it when code depends on IDateTimeService.
/// It mirrors the shared service contract without using the system clock.
/// It is test infrastructure and is not registered by production hosts.
/// </summary>
public class MockDateTimeService : IDateTimeService
{
	private static readonly DateTime DateTimeNow = new (2024, 4, 1);

	public virtual DateTime Now { get; } = DateTimeNow.ToLocalTime();

	public virtual DateTime UtcNow => DateTimeNow;

	public virtual DateTime Today { get; } =
		new (DateTimeNow.ToLocalTime().Year, DateTimeNow.ToLocalTime().Month, DateTimeNow.ToLocalTime().Day);

	public virtual DateTime MinValue { get; } = new (1901, 1, 1);

	public virtual DateTime MaxValue { get; } = new (2099, 12, 31);
}
