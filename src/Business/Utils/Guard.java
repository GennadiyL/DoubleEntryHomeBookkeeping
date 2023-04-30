package Business.Utils;

import Common.DataAccess.Base.*;
import Common.DataAccess.*;
import Common.Models.Interfaces.*;
import org.jetbrains.annotations.*;

import java.util.*;

public class Guard {
    public static <T> void checkInputForNull(T entity) {
        if (entity == null) {
            String typeName = entity.getClass().getSimpleName();
            throw new NullPointerException(typeName + " cannot be a null");
        }
    }

    public static <T extends INamedEntity> void checkInputNameForNull(@NotNull T entity) {
        if (entity.getName() == null) {
            String typeName = entity.getClass().getSimpleName();
            throw new NullPointerException("In " + typeName + " the Name cannot be a null");
        }
    }

    public static void checkEntityWithSameId(@NotNull IGlobalDataAccess dataAccess, UUID entityId) {
        if (dataAccess.Get(entityId) != null) {
            throw new IllegalArgumentException("Entity with the same Id has already existed");
        }
    }

    public static <T extends IEntity> @NotNull T checkAndGetEntityById
            (@NotNull IEntityDataAccess<T> dataAccess, UUID entityId) {
        T entity = dataAccess.get(entityId);
        if (entity == null) {
            String typeName = entity.getClass().getSimpleName();
            throw new NoSuchElementException("Entity " + typeName + " does not exist");
        }
        return entity;
    }

    public static <T extends IEntity & INamedEntity, TChild extends IEntity> void checkentitywithsamename
            (@NotNull IParentEntityDataAccess<T, TChild> query, @NotNull T entity) {
        ArrayList<T> entities = query.getByName(entity.getName());
        checkEntityWithSameName(entities, entity);
    }

    public static <T extends IEntity & INamedEntity, TParent extends IEntity> void checkEntityWithSameName
            (@NotNull IChildEntityDataAccess<T, TParent> query, UUID parentId, @NotNull T entity) {
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

    public static <T extends IEntity & INamedEntity, TParent extends IEntity> void checkexistedchildreninthegroup
            (@NotNull IChildEntityDataAccess<T, TParent> query, UUID parentId) {
        if (query.getCount(parentId) > 0) {
            throw new UnsupportedOperationException("Parent cannot be deleted. It contains items");
        }
    }
}
