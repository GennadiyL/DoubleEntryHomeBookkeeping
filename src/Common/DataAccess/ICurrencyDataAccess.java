package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.Currency;

import java.util.*;

public interface ICurrencyDataAccess
        extends IEntityDataAccess<Currency> {

    ArrayList<Currency> getByIsoCode(String isoCode);
    int getMaxOrder();
    int getCount();
    ArrayList<Currency> getList();
}
