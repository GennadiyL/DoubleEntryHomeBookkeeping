package Common.DataAccess;

import java.time.LocalDate;

public interface ISystemConfigDataAccess {
    String GetMainCurrencyIsoCode();
    LocalDate GetMinDate();
    LocalDate GetMaxDate();
}
