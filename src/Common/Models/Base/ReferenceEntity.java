package Common.Models.Base;

import Common.Models.Interfaces.IReferenceEntity;

public class ReferenceEntity extends Entity implements IReferenceEntity {

    private boolean isFavorite;
    private String name;
    private String description;
    private  int order;
    private String timeStamp;

    public boolean getIsFavorite() {
        return this.isFavorite;
    }
    public void setIsFavorite(boolean value) {
        this.isFavorite = value;
    }

    public String getName() {
        return this.name;
    }
    public void setName(String value) {
        this.name = value;
    }

    public String getDescription() {
        return this.description;
    }
    public void setDescription(String value) {
        this.description = value;
    }

    public int getOrder() {
        return this.order;
    }
    public void setOrder(int value) {
        this.order = value;
    }

    public String getTimeStamp() {
        return this.timeStamp;
    }
    public void setTimeStamp(String value) {
        this.timeStamp = value;
    }
}
