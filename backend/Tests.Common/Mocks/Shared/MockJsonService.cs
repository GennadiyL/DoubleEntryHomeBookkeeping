using System.Text.Json;
using Shared.Contracts;

namespace Tests.Common.Mocks.Shared;

public class MockJsonService : IJsonService
{
	public virtual string Serialize<T>(T obj, JsonSerializerOptions? options = null) where T : class => string.Empty;

	public virtual T? Deserialize<T>(string s, JsonSerializerOptions? options = null) where T : class => null;
}
