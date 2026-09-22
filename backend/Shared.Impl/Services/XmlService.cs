using System.Runtime.Serialization;
using System.Xml;
using System.Xml.Linq;
using Shared.Contracts;

namespace Shared.Impl.Services;

/// <summary>
/// Defines the XML service.
/// Serializes and deserializes XML through the shared project-independent contract.
/// Consumers resolve IXmlService for object, text, and stream conversion operations.
/// It centralizes platform XML serializer usage for replaceability and testing.
/// It does not define business XML schemas.
/// </summary>
internal class XmlService : IXmlService
{
	public string Serialize<T>(T? obj) where T : class
	{
		if (obj == null)
		{
			return string.Empty;
		}

		DataContractSerializer serializer = new(typeof(T));

		XmlWriterSettings settings = new()
		{
			Indent = true,
			IndentChars = "  ",
			NewLineChars = Environment.NewLine,
			NewLineHandling = NewLineHandling.Replace
		};

		using StringWriter stringWriter = new();
		using XmlWriter xmlWriter = XmlWriter.Create(stringWriter, settings);
		serializer.WriteObject(xmlWriter, obj);
		xmlWriter.Flush();
		return stringWriter.ToString();
	}

	public T? Deserialize<T>(string s) where T : class
	{
		if (string.IsNullOrEmpty(s))
		{
			return null;
		}

		DataContractSerializer serializer = new(typeof(T));
		using StringReader stringReader = new(s);
		using XmlReader xmlReader = XmlReader.Create(stringReader);
		T? obj = (T?)serializer.ReadObject(xmlReader);
		return obj;
	}

	public XDocument Load(string uri)
	{
		XDocument xDoc = XDocument.Load(uri);
		return xDoc;
	}

	public XDocument Load(Stream stream)
	{
		XDocument xDoc = XDocument.Load(stream);
		return xDoc;
	}

	public XDocument Load(TextReader reader)
	{
		XDocument xDoc = XDocument.Load(reader);
		return xDoc;
	}

	public XDocument Load(XmlReader reader)
	{
		XDocument xDoc = XDocument.Load(reader);
		return xDoc;
	}

	public virtual void Save(string uri, XDocument xDoc) => xDoc.Save(uri);

	public virtual void Save(Stream stream, XDocument xDoc) => xDoc.Save(stream);

	public virtual void Save(TextWriter writer, XDocument xDoc) => xDoc.Save(writer);

	public virtual void Save(XmlWriter writer, XDocument xDoc) => xDoc.Save(writer);

	public XDocument Parse(string text)
	{
		XDocument xDoc = XDocument.Parse(text);
		return xDoc;
	}
}
