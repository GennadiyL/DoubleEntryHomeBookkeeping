package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.Currency;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbCurrencyDataAccess
        extends MemoryDbEntityDataAccess<Currency>
        implements ICurrencyDataAccess {
    public MemoryDbCurrencyDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    protected ArrayList<Currency> getEntities() {
        return this.getLedger().getCurrencies();
    }
}
