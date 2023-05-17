package Common.DataAccess;

import java.time.*;

public interface ISystemConfigDataAccess {
    String getMainCurrencyIsoCode();
    LocalDateTime getMinDate();
    LocalDateTime getMaxDate();
}
