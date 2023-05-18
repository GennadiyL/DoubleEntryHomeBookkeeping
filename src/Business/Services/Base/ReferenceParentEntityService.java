package Business.Services.Base;

import Business.Utils.*;
import Common.DataAccess.Base.*;
import Common.DataAccess.*;
import Common.Models.Interfaces.*;
import Common.Services.Base.*;
import Common.Utils.Misk.GenericHelper;
import Common.Utils.OrderedEntity.OrderedEntityHelper;

import java.util.*;

public abstract class ReferenceParentEntityService<T extends IReferenceParentEntity<TChild>, TChild extends IReferenceChildEntity<T>>
        implements IReferenceEntityService<T>{
    private final IGlobalDataAccess globalDataAccess;
    private final IReferenceParentEntityDataAccess<T> entityDataAccess;
    private final IReferenceChildEntityDataAccess<TChild> childEntityDataAccess;

    public ReferenceParentEntityService(
            IGlobalDataAccess globalDataAccess,
            IReferenceParentEntityDataAccess<T> entityDataAccess,
            IReferenceChildEntityDataAccess<TChild> childEntityDataAccess) {
        this.globalDataAccess = globalDataAccess;
        this.entityDataAccess = entityDataAccess;
        this.childEntityDataAccess = childEntityDataAccess;
    }

    public void add(T entity) {
        Guard.checkEntityForNull(entity);
        Guard.checkEntityNameForNull(entity);
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());
        Guard.checkEntityWithSameName(this.entityDataAccess, entity);

        T addedEntity = (T) GenericHelper.createGenericType(this);
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.entityDataAccess.getMaxOrder() + 1);

        this.entityDataAccess.add(addedEntity);

        this.globalDataAccess.save();
    }

    public void update(T entity) {
        Guard.checkEntityForNull(entity);
        Guard.checkEntityNameForNull(entity);
        Guard.checkEntityWithSameName(this.entityDataAccess, entity);

        T updatedEntity = Guard.checkAndGetEntityById(this.entityDataAccess, entity.getId());
        updatedEntity.setName(entity.getName());
        updatedEntity.setDescription(entity.getDescription());
        updatedEntity.setIsFavorite(entity.getIsFavorite());

        this.entityDataAccess.update(updatedEntity);

        this.globalDataAccess.save();
    }

    public void delete(UUID entityId) {
        Guard.checkObjectForNull(entityId, "entityId");
        T deletedEntity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        Guard.checkExistedChildrenInTheGroup(this.childEntityDataAccess, deletedEntity.getId());

        this.entityDataAccess.delete(deletedEntity);

        ArrayList<T> list = this.entityDataAccess.getList();
        OrderedEntityHelper.reorder(list);
        this.entityDataAccess.updateList(list);

        this.globalDataAccess.save();

    }

    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        Guard.checkObjectForNull(entityId, "entityId");
        T etrity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        if (etrity.getIsFavorite() != isFavorite)
        {
            etrity.setIsFavorite(isFavorite);
            this.entityDataAccess.update(etrity);
        }

        this.globalDataAccess.save();
    }


    public void setOrder(UUID entityId, int order) {
        Guard.checkObjectForNull(entityId, "entityId");
        T entity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        if (entity.getOrder() != order)
        {
            ArrayList<T> list = this.entityDataAccess.getList();
            OrderedEntityHelper.setOrder(list, entity, order);
            this.entityDataAccess.updateList(list);
        }

        this.globalDataAccess.save();
    }
}
