package Common.Models.Interfaces;

import java.util.ArrayList;

public interface IParentEntity<T> {
    //region Children
    ArrayList<T> getChildren();
    //endregion
}
