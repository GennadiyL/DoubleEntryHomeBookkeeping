using System.Xml;
using System.Xml.Linq;

namespace Shared.Contracts;

public interface IXmlService
{
	/// <summary>
	/// Performs the Serialize operation through the injectable XML serialization service contract.
	/// </summary>
	public string Serialize<T>(T obj) where T : class;
	/// <summary>
	/// Performs the Deserialize operation through the injectable XML serialization service contract.
	/// </summary>
	public T? Deserialize<T>(string s) where T : class;

	/// <summary>
	/// Performs the Load operation through the injectable XML serialization service contract.
	/// </summary>
	public XDocument Load(string uri);
	/// <summary>
	/// Performs the Load operation through the injectable XML serialization service contract.
	/// </summary>
	public XDocument Load(Stream stream);
	/// <summary>
	/// Performs the Load operation through the injectable XML serialization service contract.
	/// </summary>
	public XDocument Load(TextReader reader);
	/// <summary>
	/// Performs the Load operation through the injectable XML serialization service contract.
	/// </summary>
	public XDocument Load(XmlReader reader);
	/// <summary>
	/// Performs the Save operation through the injectable XML serialization service contract.
	/// </summary>
	public void Save(string uri, XDocument xDoc);
	/// <summary>
	/// Performs the Save operation through the injectable XML serialization service contract.
	/// </summary>
	public void Save(Stream stream, XDocument xDoc);
	/// <summary>
	/// Performs the Save operation through the injectable XML serialization service contract.
	/// </summary>
	public void Save(TextWriter writer, XDocument xDoc);
	/// <summary>
	/// Performs the Save operation through the injectable XML serialization service contract.
	/// </summary>
	public void Save(XmlWriter writer, XDocument xDoc);
	/// <summary>
	/// Performs the Parse operation through the injectable XML serialization service contract.
	/// </summary>
	public XDocument Parse(string text);

}
