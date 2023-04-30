package Common.Models.Base;

import Common.Models.Interfaces.IReferenceParentEntity;
import java.util.ArrayList;

public class ReferenceParentEntity <T> extends ReferenceEntity implements IReferenceParentEntity<T> {

    //region Children
    private ArrayList<T> children = new ArrayList<>();
    @Override
    public ArrayList<T> getChildren() {
        return this.children;
    }
    //endregion
}
