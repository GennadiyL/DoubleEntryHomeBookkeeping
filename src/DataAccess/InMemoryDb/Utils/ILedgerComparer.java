package DataAccess.InMemoryDb.Utils;

import DataAccess.InMemoryDb.Ledger;

public interface ILedgerComparer {
    boolean IsEquals(Ledger first, Ledger second);
}
