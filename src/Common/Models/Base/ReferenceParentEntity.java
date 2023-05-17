package Common.Models.Base;

import Common.Models.Interfaces.IReferenceParentEntity;
import java.util.ArrayList;

public class ReferenceParentEntity <T> extends ReferenceEntity implements IReferenceParentEntity<T> {

    private final ArrayList<T> children = new ArrayList<>();

    public ArrayList<T> getChildren() {
        return this.children;
    }

}
