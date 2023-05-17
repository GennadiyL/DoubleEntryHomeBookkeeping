package DataAccess.InMemoryDb.Json;

import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Json.Persistence.*;
import org.w3c.dom.Document;

public class JsonLedgerFactory implements ILedgerFactory {

    Ledger ledger = null;
    private final IJsonSerializer serializer;
    private final IJsonStorage storage;
    private final IJsonValidator validator;

    public JsonLedgerFactory(IJsonStorage storage, IJsonValidator validator, IJsonSerializer serializer)
    {
        this.storage = storage;
        this.validator = validator;
        this.serializer = serializer;
    }

    public Ledger receive() {
        if (ledger == null)
        {
            Document doc = storage.load();

            validator.Validate(doc);

            ledger = serializer.deserialize(doc);
        }

        return ledger;
    }

    public void send() {
        if (this.ledger == null)
        {
            throw new NullPointerException("Ledger cannot be null");
        }
        Document doc = serializer.serialize(ledger);
        storage.save(doc);
    }
}
