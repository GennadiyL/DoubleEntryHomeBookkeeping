package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.Currency;
import Common.Models.Interfaces.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;
import java.util.stream.*;

public class MemoryDbCurrencyDataAccess
        extends MemoryDbEntityDataAccess<Currency>
        implements ICurrencyDataAccess {
    public MemoryDbCurrencyDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    public ArrayList<Currency> getByIsoCode(String isoCode) {
        return this.getEntitiesStream().filter(g -> g.getIsoCode().equalsIgnoreCase(isoCode)).collect(Collectors.toCollection(ArrayList::new));
    }

    public int getMaxOrder() {
        Optional<Currency> currency = this.getEntitiesStream().max(Comparator.comparing(IOrderedEntity::getOrder));
        if(currency.isPresent()){
            return currency.get().getOrder();
        }
        return 0;
    }

    public int getCount() {
        return this.getEntities().size();
    }

    public ArrayList<Currency> getList() {
        return this.getEntities();
    }

    protected ArrayList<Currency> getEntities() {
        return this.getLedger().getCurrencies();
    }
}
