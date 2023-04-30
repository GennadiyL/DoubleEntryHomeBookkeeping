package Common.Models.Base;

import Common.Models.Interfaces.IReferenceChildEntity;

public class ReferenceChildEntity<T> extends ReferenceEntity implements IReferenceChildEntity<T> {

    //region Parent
    private T parent;
    @Override
    public T getParent() {
        return this.parent;
    }
    @Override
    public void setParent(T value) {
        this.parent = value;
    }
    //endregion
}
