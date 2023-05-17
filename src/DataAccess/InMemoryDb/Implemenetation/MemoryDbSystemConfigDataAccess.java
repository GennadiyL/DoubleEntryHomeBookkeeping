package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import DataAccess.InMemoryDb.*;

import java.time.*;

public class MemoryDbSystemConfigDataAccess
        implements ISystemConfigDataAccess {
    private final ILedgerFactory factory;

    public MemoryDbSystemConfigDataAccess(ILedgerFactory factory) {
        this.factory = factory;
    }

    public String getMainCurrencyIsoCode() {
        return factory.get().getSystemConfig().getMainCurrencyIsoCode();
    }

    public LocalDateTime getMinDate() {
        return factory.get().getSystemConfig().getMinDate();
    }

    public LocalDateTime getMaxDate() {
        return factory.get().getSystemConfig().getMaxDate();
    }
}
