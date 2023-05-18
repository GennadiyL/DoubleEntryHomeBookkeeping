package Business.Utils;

import Common.DataAccess.Base.*;
import Common.DataAccess.*;
import Common.Models.Interfaces.*;
import Common.Utils.Misk.*;

import java.util.*;

public class Guard {
    public static void checkObjectForNull(Object s, String name) {
        if (s == null) {
            throw new NullPointerException(name + " cannot be a null");
        }
    }

    public static <T extends IEntity> void checkEntityForNull(T entity) {
        if (entity == null) {
            throw new NullPointerException("Entity cannot be a null");
        }
    }

    public static <T extends INamedEntity> void checkEntityNameForNull(T entity) {
        if (entity.getName() == null) {
            String typeName = entity.getClass().getSimpleName();
            throw new NullPointerException("In " + typeName + " the Name cannot be a null");
        }
    }

    public static void checkEntityWithSameId(IGlobalDataAccess dataAccess, UUID entityId) {
        if (dataAccess.getEntity(entityId) != null) {
            throw new IllegalArgumentException("Entity with the same Id has already existed");
        }
    }

    public static <T extends IEntity>  T checkAndGetEntityById(IEntityDataAccess<T> dataAccess, UUID entityId) {
        T entity = dataAccess.get(entityId);
        if (entity == null) {
            String typeName = GenericHelper.getGenericType(dataAccess).getSimpleName();
            throw new NoSuchElementException("Entity " + typeName + " does not exist");
        }
        return entity;
    }

    public static <T extends IEntity & INamedEntity> void checkEntityWithSameName(IParentEntityDataAccess<T> query,  T entity) {
        ArrayList<T> entities = query.getByName(entity.getName());
        checkEntityWithSameName(entities, entity);
    }

    public static <T extends IEntity & INamedEntity> void checkEntityWithSameName(IChildEntityDataAccess<T> query, UUID parentId,  T entity) {
        ArrayList<T> entities = query.getByName(parentId, entity.getName());
        checkEntityWithSameName(entities, entity);
    }

    public static <T extends IEntity & INamedEntity> void checkEntityWithSameName(ArrayList<T> entities, T entity) {
        if (entities == null) {
            return;
        }
        entities.forEach((e) -> {
            if (e.getId() != e.getId() && e.getName().equalsIgnoreCase(entity.getName())) {
                String typeName = entity.getClass().getSimpleName();
                throw new IllegalArgumentException("In " + typeName + " the same name has already existed");
            }
        });
    }

    public static <T extends IEntity & INamedEntity> void checkExistedChildrenInTheGroup(IChildEntityDataAccess<T> query, UUID parentId) {
        if (query.getCount(parentId) > 0) {
            throw new UnsupportedOperationException("Parent cannot be deleted. It contains items");
        }
    }
}
