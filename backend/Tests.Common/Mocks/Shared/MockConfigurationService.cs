using Shared.Contracts.Config;

namespace Tests.Common.Mocks.Shared;

/// <summary>
/// Supplies an existing fixture directory to configuration consumers.
/// Tests seed databases in this directory before resolving their units of work.
/// Repeated requests return the fixture-owned path.
/// The fixture remains responsible for deleting its files after all providers close.
/// This test double never chooses a platform temporary directory.
/// Disposal leaves the externally owned fixture directory intact.
/// Production hosts use the scoped Shared implementation instead.
/// No application configuration or persistent user files are accessed.
/// </summary>
public sealed class MockConfigurationService : IConfigurationService
{
	private readonly string _directory;

	public MockConfigurationService(string directory) => _directory = directory;

	public string GetTempDirectory() => _directory;

	public ValueTask DisposeAsync() => ValueTask.CompletedTask;
}
