package Common.Models.Config;

import java.time.*;

public class SystemConfig {

    private String mainCurrencyIsoCode;
    private LocalDateTime minDate;
    private LocalDateTime maxDate;

    public String getMainCurrencyIsoCode() {
        return mainCurrencyIsoCode;
    }
    public void setMainCurrencyIsoCode(String mainCurrencyIsoCode) {
        this.mainCurrencyIsoCode = mainCurrencyIsoCode;
    }

    public LocalDateTime getMinDate() {
        return minDate;
    }
    public void setMinDate(LocalDateTime minDate) {
        this.minDate = minDate;
    }

    public LocalDateTime getMaxDate() {
        return maxDate;
    }
    public void setMaxDate(LocalDateTime maxDate) {
        this.maxDate = maxDate;
    }
}
