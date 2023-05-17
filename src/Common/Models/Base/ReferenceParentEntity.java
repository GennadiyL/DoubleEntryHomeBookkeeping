package Common.Models.Base;

import Common.Models.Interfaces.*;
import java.util.*;

public class ReferenceParentEntity <T> extends ReferenceEntity implements IReferenceParentEntity<T> {

    private final ArrayList<T> children = new ArrayList<>();

    public ArrayList<T> getChildren() {
        return this.children;
    }

}
