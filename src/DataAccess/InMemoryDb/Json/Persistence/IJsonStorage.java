package DataAccess.InMemoryDb.Json.Persistence;

import org.w3c.dom.Document;

public interface IJsonStorage {
    Document load();
    void save(Document doc);

}
