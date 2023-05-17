package Business.Services.Base;

import Common.Utils.Misk.CreateHelper;
import Business.Utils.Guard;
import Common.Utils.OrderedEntity.OrderedEntityHelper;
import Common.DataAccess.Base.*;
import Common.DataAccess.*;
import Common.Models.Account;
import Common.Models.Interfaces.*;
import Common.Services.Base.*;

import java.util.*;

public abstract class ReferenceChildEntityService<T extends IReferenceChildEntity<TParent>, TParent extends IReferenceParentEntity<T>>
        implements IReferenceChildEntityService<T> {

    private final IGlobalDataAccess globalDataAccess;
    private final IReferenceChildEntityDataAccess<T> entityDataAccess;
    private final IReferenceParentEntityDataAccess<TParent> parentEntityDataAccess;
    private final IAccountDataAccess accountDataAccess;

    public ReferenceChildEntityService(
            IGlobalDataAccess globalDataAccess,
            IReferenceChildEntityDataAccess<T> entityDataAccess,
            IReferenceParentEntityDataAccess<TParent> parentEntityDataAccess,
            IAccountDataAccess accountDataAccess)
    {
        this.globalDataAccess = globalDataAccess;
        this.entityDataAccess = entityDataAccess;
        this.parentEntityDataAccess = parentEntityDataAccess;
        this.accountDataAccess = accountDataAccess;
    }

    protected IAccountDataAccess getAccountDataAccess() { return this.accountDataAccess; }
    protected abstract ArrayList<Account> GetAccountsByEntity(T entity);
    protected abstract void AccountEntitySetter(T entity, Account account);

    @Override
    public void add(T entity) {
        this.globalDataAccess.load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());

        TParent parent = Guard.checkAndGetEntityById(this.parentEntityDataAccess, entity.getParent().getId());
        Guard.checkEntityWithSameName(this.entityDataAccess, parent.getId(), entity);

        T addedEntity = (T) CreateHelper.createGenericType(this);
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.entityDataAccess.getMaxOrder(parent.getId()) + 1);

        addedEntity.setParent(parent);
        parent.getChildren().add(addedEntity);

        this.entityDataAccess.add(addedEntity);
        this.globalDataAccess.save();
    }

    @Override
    public void update(T entity) {
        this.globalDataAccess.load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());

        T updatedEntity = Guard.checkAndGetEntityById(this.entityDataAccess, entity.getId());
        this.entityDataAccess.loadParent(updatedEntity);
        TParent parent = updatedEntity.getParent();
        Guard.checkEntityWithSameName(this.entityDataAccess, parent.getId(), entity);

        updatedEntity.setName(entity.getName());
        updatedEntity.setDescription(entity.getDescription());
        updatedEntity.setIsFavorite(entity.getIsFavorite());

        this.entityDataAccess.update(updatedEntity);
        this.globalDataAccess.save();
    }

    @Override
    public void delete(UUID entityId) {
        this.globalDataAccess.load();

        T deletedEntity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        this.entityDataAccess.loadParent(deletedEntity);
        TParent parent = deletedEntity.getParent();
        this.parentEntityDataAccess.loadChildren(parent);

        List<Account> accounts = this.GetAccountsByEntity(deletedEntity);
        for(Account account : accounts)
        {
            this.AccountEntitySetter(deletedEntity, null);
            this.accountDataAccess.update(account);
        }

        parent.getChildren().remove(deletedEntity);
        this.entityDataAccess.delete(deletedEntity);

        OrderedEntityHelper.reorder(parent.getChildren());
        this.entityDataAccess.updateList(parent.getChildren());

        this.globalDataAccess.save();
    }

    @Override
    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        this.globalDataAccess.load();

        T etrity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        if (etrity.getIsFavorite() != isFavorite)
        {
            etrity.setIsFavorite(isFavorite);
            this.entityDataAccess.update(etrity);
        }

        this.globalDataAccess.save();
    }

    @Override
    public void setOrder(UUID entityId, int order) {
        this.globalDataAccess.load();

        T entity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        if (entity.getOrder() != order)
        {
            this.entityDataAccess.loadParent(entity);
            TParent parent = entity.getParent();
            this.parentEntityDataAccess.loadChildren(parent);
            OrderedEntityHelper.setOrder(parent.getChildren(), entity, order);
            this.entityDataAccess.updateList(parent.getChildren());
        }

        this.globalDataAccess.save();
    }
    @Override
    public void moveToAnotherParent(UUID entityId, UUID parentId) {
        this.globalDataAccess.load();

        T entity = Guard.checkAndGetEntityById(this.entityDataAccess, entityId);
        this.entityDataAccess.loadParent(entity);
        TParent fromParent = entity.getParent();
        TParent toParent = Guard.checkAndGetEntityById(this.parentEntityDataAccess, parentId);

        if (fromParent.getId() != toParent.getId())
        {
            Guard.checkEntityWithSameName(this.entityDataAccess, toParent.getId(), entity);
            this.parentEntityDataAccess.loadChildren(fromParent);

            entity.setOrder(this.entityDataAccess.getMaxOrder(toParent.getId()) + 1);
            entity.setParent(toParent);
            toParent.getChildren().add(entity);
            this.entityDataAccess.update(entity);

            fromParent.getChildren().remove(entity);
            OrderedEntityHelper.reorder(fromParent.getChildren());
            this.entityDataAccess.updateList(fromParent.getChildren());
        }

        this.globalDataAccess.save();
    }

    public void combineTwoEntities(UUID primaryId, UUID secondaryId) {
        this.globalDataAccess.load();

        T primaryItem = Guard.checkAndGetEntityById(this.entityDataAccess, primaryId);
        T secondaryItem = Guard.checkAndGetEntityById(this.entityDataAccess, secondaryId);

        if (primaryItem.getId() != secondaryItem.getId())
        {
            List<Account> accounts = this.GetAccountsByEntity(secondaryItem);
            for (Account account : accounts)
            {
                this.AccountEntitySetter(primaryItem, account);
                this.accountDataAccess.update(account);
            }

            this.entityDataAccess.loadParent(secondaryItem);
            TParent parent = secondaryItem.getParent();
            this.parentEntityDataAccess.loadChildren(parent);

            parent.getChildren().remove(secondaryItem);
            this.entityDataAccess.delete(secondaryItem);

            OrderedEntityHelper.reorder(parent.getChildren());
            this.entityDataAccess.updateList(parent.getChildren());
        }

        this.globalDataAccess.save();
    }
}
