package Common.Models;

import Common.Models.Base.Entity;
import Common.Models.Interfaces.ITrackedEntity;

import java.math.*;
import java.time.LocalDate;

public class CurrencyRate extends Entity implements ITrackedEntity {

    //region TimeStamp
    private String timeStamp;
    public String getTimeStamp() {
        return this.timeStamp;
    }
    public void setTimeStamp(String value) {
        this.timeStamp = value;
    }
    //endregion

    //region Currency
    private Currency currency;
    public Currency getCurrency() {
        return this.currency;
    }
    public void setCurrency(Currency currency) {
        this.currency = currency;
    }
    //endregion

    //region Date
    private LocalDate date;
    public LocalDate getDate() {
        return this.date;
    }
    public void setDate(LocalDate date) {
        this.date = date;
    }
    //endregion

    //region Rate
    private BigDecimal rate;
    public BigDecimal getRate() {
        return this.rate;
    }
    public void setRate(BigDecimal rate) {
        this.rate = rate;
    }
    //endregion

    //region Comment
    private String comment;
    public String getComment() {
        return this.comment;
    }
    public void setComment(String comment) {
        this.comment = comment;
    }
    //endregion
}
