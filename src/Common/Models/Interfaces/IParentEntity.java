package Common.Models.Interfaces;

import java.util.ArrayList;

public interface IParentEntity<T> {

    ArrayList<T> getChildren();

}
