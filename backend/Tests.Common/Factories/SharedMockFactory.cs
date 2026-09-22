using Tests.Common.Mocks.Shared;

namespace Tests.Common.Factories;

/// <summary>
/// Defines the shared test-double factory.
/// Creates common shared-service test doubles with their required dependencies.
/// Tests call the properties to obtain fresh mock service instances.
/// It centralizes routine construction of Tests.Common mock classes.
/// It does not configure a production dependency injection container.
/// </summary>
public static class SharedMockFactory
{
	public static MockDateTimeService DateTimeService => new ();
	public static MockFileService FileService => new (new MockDateTimeService());
	public static MockDirectoryService DirectoryService => new (new MockDateTimeService());
}
