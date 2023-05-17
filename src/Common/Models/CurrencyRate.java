package Common.Models;

import Common.Models.Base.Entity;
import Common.Models.Interfaces.ITrackedEntity;

import java.math.*;
import java.time.LocalDate;

public class CurrencyRate extends Entity implements ITrackedEntity {

    private String timeStamp;
    private Currency currency;
    private LocalDate date;
    private BigDecimal rate;
    private String comment;

    public String getTimeStamp() {
        return this.timeStamp;
    }
    public void setTimeStamp(String value) {
        this.timeStamp = value;
    }

    public Currency getCurrency() {
        return this.currency;
    }
    public void setCurrency(Currency currency) {
        this.currency = currency;
    }

    public LocalDate getDate() {
        return this.date;
    }
    public void setDate(LocalDate date) {
        this.date = date;
    }

    public BigDecimal getRate() {
        return this.rate;
    }
    public void setRate(BigDecimal rate) {
        this.rate = rate;
    }

    public String getComment() {
        return this.comment;
    }
    public void setComment(String comment) {
        this.comment = comment;
    }
}
