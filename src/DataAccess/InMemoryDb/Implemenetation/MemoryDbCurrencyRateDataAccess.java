package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbCurrencyRateDataAccess extends MemoryDbEntityDataAccess<CurrencyRate> implements ICurrencyRateDataAccess {
    public MemoryDbCurrencyRateDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    @Override
    protected ArrayList<CurrencyRate> getEntities() {
        return this.getLedger().getCurrencyRates();
    }
}
