package Common.Models.Base;

import Common.Models.Interfaces.IReferenceChildEntity;

public class ReferenceChildEntity<T> extends ReferenceEntity implements IReferenceChildEntity<T> {

    private T parent;

    public T getParent() {
        return this.parent;
    }
    public void setParent(T value) {
        this.parent = value;
    }

}
