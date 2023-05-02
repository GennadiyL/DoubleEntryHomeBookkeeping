package DataAccess.InMemoryDb.Xml.Persistence;

import org.w3c.dom.Document;

public interface IXmlStorage {
    Document load();
    void save(Document doc);

}
