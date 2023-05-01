package Business.Services.Base;

import Business.Utils.Creator;
import Business.Utils.Guard;
import Business.Utils.OrderedUtils;
import Common.DataAccess.Base.*;
import Common.DataAccess.*;
import Common.Models.Account;
import Common.Models.Interfaces.*;
import Common.Services.Base.*;

import java.util.*;

public abstract class ReferenceChildEntityService<T extends IReferenceChildEntity<TParent>, TParent extends IReferenceParentEntity<T>>
        implements IReferenceChildEntityService<T> {

    private final IGlobalDataAccess globalDataAccess;
    private final IReferenceChildEntityDataAccess<T, TParent> entityDataAccess;
    private final IReferenceParentEntityDataAccess<TParent, T> parentEntityDataAccess;
    private final IAccountDataAccess accountDataAccess;

    public ReferenceChildEntityService(
            IGlobalDataAccess globalDataAccess,
            IReferenceChildEntityDataAccess<T, TParent> entityDataAccess,
            IReferenceParentEntityDataAccess<TParent, T> parentEntityDataAccess,
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
        this.globalDataAccess.Load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());

        TParent parent = Guard.checkAndGetEntityById(this.parentEntityDataAccess, entity.getParent().getId());
        Guard.checkEntityWithSameName(this.entityDataAccess, parent.getId(), entity);

        T addedEntity = (T) Creator.createGenericType(this);
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.entityDataAccess.getMaxOrder(parent.getId()) + 1);

        addedEntity.setParent(parent);
        parent.getChildren().add(addedEntity);

        this.entityDataAccess.add(addedEntity);
        this.globalDataAccess.Save();
    }

    @Override
    public void update(T entity) {
        this.globalDataAccess.Load();

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
        this.globalDataAccess.Save();
    }

    @Override
    public void delete(UUID entityId) {
        this.globalDataAccess.Load();

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

        OrderedUtils.reorder(parent.getChildren());
        this.entityDataAccess.updateList(parent.getChildren());

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
            this.entityDataAccess.loadParent(entity);
            TParent parent = entity.getParent();
            this.parentEntityDataAccess.loadChildren(parent);
            OrderedUtils.setOrder(parent.getChildren(), entity, order);
            this.entityDataAccess.updateList(parent.getChildren());
        }

        this.globalDataAccess.Save();
    }
    @Override
    public void moveToAnotherParent(UUID entityId, UUID parentId) {
        this.globalDataAccess.Load();

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
            OrderedUtils.reorder(fromParent.getChildren());
            this.entityDataAccess.updateList(fromParent.getChildren());
        }

        this.globalDataAccess.Save();
    }

    public void combineTwoEntities(UUID primaryId, UUID secondaryId) {
        this.globalDataAccess.Load();

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

            OrderedUtils.reorder(parent.getChildren());
            this.entityDataAccess.updateList(parent.getChildren());
        }

        this.globalDataAccess.Save();
    }
}
