package Common.DataAccess.Base;

import Common.Models.Interfaces.IEntity;
import Common.Models.Interfaces.INamedEntity;

import java.util.ArrayList;

public interface IParentEntityDataAccess<T extends IEntity & INamedEntity> extends IParentDataAccess<T> {
    ArrayList<T> getByName(String name);
    int getMaxOrder();
    int getCount();
    ArrayList<T> getList();
}
