package Common.Models.Base;

import Common.Models.Interfaces.IReferenceEntity;

public class ReferenceEntity extends Entity implements IReferenceEntity {

    //region IsFavorite
    private boolean isFavorite;
    @Override
    public boolean getIsFavorite() {
        return this.isFavorite;
    }
    @Override
    public void setIsFavorite(boolean value) {
        this.isFavorite = value;
    }
    //endregion

    //region Name
    private String name;
    @Override
    public String getName() {
        return this.name;
    }
    @Override
    public void setName(String value) {
        this.name = value;
    }
    //endregion

    //region Desciption
    private String description;
    @Override
    public String getDescription() {
        return this.description;
    }
    @Override
    public void setDescription(String value) {
        this.description = value;
    }
    //endregion

    //region Order
    private  int order;
    @Override
    public int getOrder() {
        return this.order;
    }
    @Override
    public void setOrder(int value) {
        this.order = value;
    }
    //endregion

    //region TimeStamp
    private String timeStamp;
    @Override
    public String getTimeStamp() {
        return this.timeStamp;
    }
    @Override
    public void setTimeStamp(String value) {
        this.timeStamp = value;
    }
    //endregion
}
