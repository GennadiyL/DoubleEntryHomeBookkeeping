using System.Text.Json;

namespace Shared.Contracts;

public interface IJsonService
{
	/// <summary>
	/// Performs the Serialize operation through the injectable JSON serialization service contract.
	/// </summary>
	public string Serialize<T>(T obj, JsonSerializerOptions? options = null) where T : class;
	/// <summary>
	/// Performs the Deserialize operation through the injectable JSON serialization service contract.
	/// </summary>
	public T? Deserialize<T>(string s, JsonSerializerOptions? options = null) where T : class;
}
