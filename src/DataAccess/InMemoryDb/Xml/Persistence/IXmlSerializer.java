package DataAccess.InMemoryDb.Xml.Persistence;

import DataAccess.InMemoryDb.Ledger;
import org.w3c.dom.Document;

public interface IXmlSerializer {
    Document serialize(Ledger ledger);
    Ledger deserialize(Document document);

}
