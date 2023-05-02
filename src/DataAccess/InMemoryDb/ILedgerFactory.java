package DataAccess.InMemoryDb;

public interface ILedgerFactory {
    Ledger get();
    void set(Ledger ledger);
}
