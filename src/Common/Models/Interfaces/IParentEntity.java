package Common.Models.Interfaces;

import java.util.*;

public interface IParentEntity<T> {
    ArrayList<T> getChildren();
}
