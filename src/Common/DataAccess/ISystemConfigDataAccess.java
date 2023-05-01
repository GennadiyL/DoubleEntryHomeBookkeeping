package Common.DataAccess;

import java.time.LocalDate;
import java.time.LocalDateTime;

public interface ISystemConfigDataAccess {
    String getMainCurrencyIsoCode();
    LocalDateTime getMinDate();
    LocalDateTime getMaxDate();
}
