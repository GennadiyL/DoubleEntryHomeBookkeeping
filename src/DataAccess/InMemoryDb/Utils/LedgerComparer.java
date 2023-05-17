package DataAccess.InMemoryDb.Utils;

import DataAccess.InMemoryDb.Ledger;
import DataAccess.InMemoryDb.Utils.ILedgerComparer;

public class LedgerComparer implements ILedgerComparer {
    
    public boolean IsEquals(Ledger first, Ledger second)
    {
        return true;
    }
}
