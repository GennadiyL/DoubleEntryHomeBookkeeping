package DataAccess.InMemoryDb;

public interface ILedgerFactory {
    Ledger receive();
    void send();
}
