package Common.Models;

import Common.Models.Base.Entity;
import Common.Models.Interfaces.*;

import java.util.ArrayList;

public class Currency extends Entity implements ITrackedEntity, IFavoriteEntity, IOrderedEntity {

    private String timeStamp;
    private int order;
    private boolean isFavorite;
    private String isoCode;
    private String symbol;
    private String description;
    private final ArrayList<CurrencyRate> rates = new ArrayList<>();

    public String getTimeStamp() {
        return this.timeStamp;
    }
    public void setTimeStamp(String timeStamp) {
        this.timeStamp = timeStamp;
    }

    public int getOrder() {
        return this.order;
    }
    public void setOrder(int order) {
        this.order = order;
    }

    public boolean getIsFavorite() {
        return this.isFavorite;
    }
    public void setIsFavorite(boolean favorite) {
        this.isFavorite = favorite;
    }

    public String getIsoCode() {
        return this.isoCode;
    }
    public void setIsoCode(String isoCode) {
        this.isoCode = isoCode;
    }

    public String getSymbol() {
        return this.symbol;
    }
    public void setSymbol(String symbol) {
        this.symbol = symbol;
    }

    public String geDescription() {
        return this.description;
    }
    public void setDescription(String description) {
        this.description = description;
    }

    public ArrayList<CurrencyRate> getRates() {
        return this.rates;
    }
}
