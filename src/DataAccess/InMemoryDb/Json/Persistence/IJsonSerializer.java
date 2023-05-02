package DataAccess.InMemoryDb.Json.Persistence;

import DataAccess.InMemoryDb.Ledger;
import org.w3c.dom.Document;

public interface IJsonSerializer {
    Document serialize(Ledger ledger);
    Ledger deserialize(Document document);

}
