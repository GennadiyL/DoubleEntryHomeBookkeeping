package Common.DataAccess.Base;

import Common.Models.Interfaces.IEntity;
import Common.Models.Interfaces.INamedEntity;

import java.util.ArrayList;
import java.util.UUID;

public interface IChildEntityDataAccess<T extends IEntity & INamedEntity, TParent extends IEntity>  {
    ArrayList<T> getByName(UUID parentId, String name);
    int getMaxOrder(UUID parentId);
    int getCount(UUID parentId);
    void loadParent(T entity);
}
