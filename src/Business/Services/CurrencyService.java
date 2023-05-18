package Business.Services;

import Business.Utils.*;
import Common.DataAccess.*;
import Common.Services.*;

import java.util.*;

public class CurrencyService  implements ICurrencyService {
    private final IGlobalDataAccess globalDataAccess;
    private final ICurrencyDataAccess currencyDataAccess;

    public CurrencyService(IGlobalDataAccess globalDataAccess, ICurrencyDataAccess currencyDataAccess) {
        this.globalDataAccess = globalDataAccess;
        this.currencyDataAccess = currencyDataAccess;
    }

    public void add(String isoCode) {
        Guard.checkObjectForNull(isoCode, "isoCode");
    }

    public void delete(String isoCode) {
        Guard.checkObjectForNull(isoCode, "isoCode");
    }

    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        Guard.checkObjectForNull(entityId, "entityId");
    }

    public void setOrder(UUID entityId, int order) {
        Guard.checkObjectForNull(entityId, "entityId");
    }

}
