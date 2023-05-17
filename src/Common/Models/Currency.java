package Common.Models;

import Common.Models.Base.Entity;
import Common.Models.Interfaces.*;

import java.util.ArrayList;

public class Currency extends Entity implements ITrackedEntity, IFavoriteEntity, IOrderedEntity {

    //region TimeStamp
    String timeStamp;
    @Override
    public String getTimeStamp() {
        return this.timeStamp;
    }
    @Override
    public void setTimeStamp(String timeStamp) {
        this.timeStamp = timeStamp;
    }
    //endregion

    //region Order
    int order;
    @Override
    public int getOrder() {
        return this.order;
    }
    @Override
    public void setOrder(int order) {
        this.order = order;
    }
    //endregion

    //region IsFavorite
    boolean isFavorite;
    @Override
    public boolean getIsFavorite() {
        return this.isFavorite;
    }
    @Override
    public void setIsFavorite(boolean favorite) {
        this.isFavorite = favorite;
    }
    //endregion

    //region IsoCode
    String isoCode;
    public String getIsoCode() {
        return this.isoCode;
    }
    public void setIsoCode(String isoCode) {
        this.isoCode = isoCode;
    }
    //endregion

    //region Symbol
    String symbol;
    public String getSymbol() {
        return this.symbol;
    }
    public void setSymbol(String symbol) {
        this.symbol = symbol;
    }
    //endregion

    //region Desctipton
    String desctipton;
    public String geDesctipton() {
        return this.desctipton;
    }
    public void setDesctipton(String desctipton) {
        this.desctipton = desctipton;
    }
    //endregion-

    //region Rates
    ArrayList<CurrencyRate> rates;
    public ArrayList<CurrencyRate> getRates() {
        return this.rates;
    }
    //endregion
}
