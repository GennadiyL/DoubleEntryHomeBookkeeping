package Common.DataAccess.Base;

import Common.Models.Interfaces.IEntity;

import java.util.ArrayList;
import java.util.UUID;

public interface IEntityDataAccess<T extends IEntity> {
    T get(UUID id);

    void add(T entity);
    void addList(ArrayList<T> list);

    void update(T entity);
    void updateList(ArrayList<T> list);


    void delete(T entity);
    void deleteList(ArrayList<T> list);
}
