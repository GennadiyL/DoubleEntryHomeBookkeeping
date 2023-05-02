package DataAccess.InMemoryDb.Xml;

import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Xml.Persistence.*;
import org.w3c.dom.Document;

public class XmlLedgerFactory implements ILedgerFactory {

    Ledger ledger = null;
    private final IXmlSerializer serializer;
    private final IXmlStorage storage;
    private final IXmlValidator validator;

    public XmlLedgerFactory(IXmlStorage storage, IXmlValidator validator, IXmlSerializer serializer)
    {
        this.storage = storage;
        this.validator = validator;
        this.serializer = serializer;
    }

    @Override
    public Ledger get() {
        if (ledger == null)
        {
            Document doc = storage.load();

            validator.Validate(doc);

            ledger = serializer.deserialize(doc);
        }

        return ledger;
    }

    @Override
    public void set(Ledger ledger) {
        if (ledger == null)
        {
            throw new NullPointerException("Ledger cannot be null");
        }
        this.ledger = ledger;
        Document doc = serializer.serialize(ledger);
        storage.save(doc);
    }
}
