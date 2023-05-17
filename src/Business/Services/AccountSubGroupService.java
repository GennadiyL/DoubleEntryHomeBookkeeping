package Business.Services;

import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;
import Business.Utils.*;
import Common.Utils.OrderedEntity.OrderedEntityHelper;

import java.util.*;

public class AccountSubGroupService implements IAccountSubGroupService {
    private final IGlobalDataAccess globalDataAccess;
    private final IAccountDataAccess accountDataAccess;
    private final IAccountSubGroupDataAccess accountSubGroupDataAccess;
    private final IAccountGroupDataAccess accountGroupDataAccess;

    public AccountSubGroupService(
            IGlobalDataAccess globalDataAccess,
            IAccountDataAccess accountDataAccess,
            IAccountSubGroupDataAccess accountSubGroupDataAccess,
            IAccountGroupDataAccess accountGroupDataAccess) {
        this.globalDataAccess = globalDataAccess;
        this.accountDataAccess = accountDataAccess;
        this.accountSubGroupDataAccess = accountSubGroupDataAccess;
        this.accountGroupDataAccess = accountGroupDataAccess;
    }

    public void add(AccountSubGroup entity) {
        this.globalDataAccess.load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());

        AccountGroup group = Guard.checkAndGetEntityById(this.accountGroupDataAccess, entity.getParent().getId());
        Guard.checkEntityWithSameName(this.accountSubGroupDataAccess, group.getId(), entity);

        AccountSubGroup addedEntity = new AccountSubGroup();
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.accountSubGroupDataAccess.getMaxOrder(addedEntity.getId()) + 1);

        group.getChildren().add(addedEntity);
        addedEntity.setParent(group);

        this.accountSubGroupDataAccess.add(addedEntity);

        this.globalDataAccess.save();
    }

    public void update(AccountSubGroup entity) {
        this.globalDataAccess.load();

        Guard.checkInputForNull(entity);
        Guard.checkInputNameForNull(entity);
        Guard.checkInputForNull(entity.getParent());

        AccountSubGroup subGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, entity.getId());
        this.accountSubGroupDataAccess.loadParent(subGroup);
        AccountGroup parent = subGroup.getParent();

        Guard.checkEntityWithSameName(this.accountSubGroupDataAccess, parent.getId(), entity);

        subGroup.setName(entity.getName());
        subGroup.setDescription(entity.getDescription());
        subGroup.setIsFavorite(entity.getIsFavorite());

        this.accountSubGroupDataAccess.update(subGroup);

        this.globalDataAccess.save();
    }

    public void delete(UUID entityId) {
        this.globalDataAccess.load();

        AccountSubGroup subGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, entityId);
        Guard.checkExistedChildrenInTheGroup(this.accountDataAccess, subGroup.getId());

        this.accountSubGroupDataAccess.loadParent(subGroup);
        AccountGroup group = subGroup.getParent();
        this.accountGroupDataAccess.loadChildren(group);

        group.getChildren().remove(subGroup);
        this.accountSubGroupDataAccess.delete(subGroup);

        OrderedEntityHelper.reorder(group.getChildren());
        this.accountSubGroupDataAccess.updateList(group.getChildren());

        this.globalDataAccess.save();
    }

    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        this.globalDataAccess.load();

        AccountSubGroup subGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, entityId);
        if (subGroup.getIsFavorite() != isFavorite) {
            subGroup.setIsFavorite(isFavorite);
            this.accountSubGroupDataAccess.update(subGroup);
        }

        this.globalDataAccess.save();
    }

    public void setOrder(UUID entityId, int order) {
        this.globalDataAccess.load();

        AccountSubGroup subGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, entityId);
        if (subGroup.getOrder() != order) {
            this.accountSubGroupDataAccess.loadParent(subGroup);
            AccountGroup group = subGroup.getParent();
            this.accountGroupDataAccess.loadChildren(group);
            OrderedEntityHelper.setOrder(group.getChildren(), subGroup, order);
            this.accountSubGroupDataAccess.updateList(group.getChildren());
        }

        this.globalDataAccess.save();
    }

    public void moveToAnotherParent(UUID entityId, UUID groupId) {
        this.globalDataAccess.load();

        AccountSubGroup subGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, entityId);
        this.accountSubGroupDataAccess.loadParent(subGroup);
        AccountGroup fromAccountGroup = subGroup.getParent();

        AccountGroup toAccountGroup = Guard.checkAndGetEntityById(this.accountGroupDataAccess, groupId);

        if (fromAccountGroup.getId() != toAccountGroup.getId()) {
            Guard.checkEntityWithSameName(this.accountSubGroupDataAccess, toAccountGroup.getId(), subGroup);

            this.accountGroupDataAccess.loadChildren(fromAccountGroup);

            subGroup.setOrder(this.accountSubGroupDataAccess.getMaxOrder(toAccountGroup.getId()) + 1);

            fromAccountGroup.getChildren().remove(subGroup);
            toAccountGroup.getChildren().add(subGroup);
            subGroup.setParent(toAccountGroup);
            this.accountSubGroupDataAccess.update(subGroup);

            OrderedEntityHelper.reorder(fromAccountGroup.getChildren());
            this.accountSubGroupDataAccess.updateList(fromAccountGroup.getChildren());
        }

        this.globalDataAccess.save();
    }
}
