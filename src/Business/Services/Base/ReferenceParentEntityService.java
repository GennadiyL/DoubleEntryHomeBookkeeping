package Business.Services.Base;

import Business.Utils.*;
import Common.DataAccess.Base.*;
import Common.DataAccess.*;
import Common.Models.Interfaces.*;
import Common.Services.Base.*;

import java.util.*;

public abstract class ReferenceParentEntityService<T extends IReferenceParentEntity<TChild>, TChild extends IReferenceChildEntity<T>>
        implements IReferenceEntityService<T>{
    private final IGlobalDataAccess globalDataAccess;
    private final IReferenceParentEntityDataAccess<T, TChild> entityDataAccess;
    private final IReferenceChildEntityDataAccess<TChild, T> childEntityDataAccess;

    public ReferenceParentEntityService(
            IGlobalDataAccess globalDataAccess,
            IReferenceParentEntityDataAccess<T, TChild> entityDataAccess,
            IReferenceChildEntityDataAccess<TChild, T> childEntityDataAccess) {
        this.globalDataAccess = globalDataAccess;
        this.entityDataAccess = entityDataAccess;
        this.childEntityDataAccess = childEntityDataAccess;
    }

    @Override
    public void add(T entity)
    {
         this.globalDataAccess.Load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());
        Guard.checkentitywithsamename(this.entityDataAccess, entity);

        T addedEntity = (T) Creator.createGenericType(this);
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.entityDataAccess.getMaxOrder() + 1);

        this.entityDataAccess.add(addedEntity);

        this.globalDataAccess.Save();
    }

    @Override
    public void update(T entity) {
        this.globalDataAccess.Load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkentitywithsamename(this.entityDataAccess, entity);

        T updatedEntity = Guard.checkAndGetEntityById(this.entityDataAccess, entity.getId());
        updatedEntity.setName(entity.getName());
        updatedEntity.setDescription(entity.getDescription());
        updatedEntity.setIsFavorite(entity.getIsFavorite());

        this.entityDataAccess.update(updatedEntity);

        this.globalDataAccess.Save();
    }

    @Override
    public void delete(UUID entityId) {
        this.globalDataAccess.Load();

        T deletedEntity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        Guard.checkExistedChildrenInTheGroup(this.childEntityDataAccess, deletedEntity.getId());

        this.entityDataAccess.delete(deletedEntity);

        ArrayList<T> list = this.entityDataAccess.getList();
        OrderedUtils.reorder(list);
        this.entityDataAccess.updateList(list);

        this.globalDataAccess.Save();

    }

    @Override
    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        this.globalDataAccess.Load();

        T etrity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        if (etrity.getIsFavorite() != isFavorite)
        {
            etrity.setIsFavorite(isFavorite);
            this.entityDataAccess.update(etrity);
        }

        this.globalDataAccess.Save();
    }

    @Override
    public void setOrder(UUID entityId, int order) {
        this.globalDataAccess.Load();

        T entity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        if (entity.getOrder() != order)
        {
            ArrayList<T> list = this.entityDataAccess.getList();
            OrderedUtils.setOrder(list, entity, order);
            this.entityDataAccess.updateList(list);
        }

        this.globalDataAccess.Save();
    }
}
