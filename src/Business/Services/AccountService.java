package Business.Services;

import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;
import Business.Utils.*;
import Common.Utils.OrderedEntity.OrderedEntityHelper;

import java.util.*;

public class AccountService implements IAccountService {
    private final IGlobalDataAccess globalDataAccess;
    private final IAccountDataAccess accountDataAccess;
    private final IAccountSubGroupDataAccess accountSubGroupDataAccess;
    private final ICategoryDataAccess categoryDataAccess;
    private final IProjectDataAccess projectDataAccess;
    private final ICorrespondentDataAccess correspondentDataAccess;
    private final ITemplateDataAccess templateDataAccess;
    private final ITransactionDataAccess transactionDataAccess;
    private final ICurrencyDataAccess currencyDataAccess;

    public AccountService(IGlobalDataAccess globalDataAccess
            , IAccountDataAccess accountDataAccess
            , IAccountSubGroupDataAccess subGroupDataAccess
            , ICategoryDataAccess categoryDataAccess
            , IProjectDataAccess projectDataAccess
            , ICorrespondentDataAccess correspondentDataAccess
            , ITemplateDataAccess templateDataAccess
            , ITransactionDataAccess transactionDataAccess
            , ICurrencyDataAccess currencyDataAccess) {
        this.globalDataAccess = globalDataAccess;
        this.accountDataAccess = accountDataAccess;
        this.accountSubGroupDataAccess = subGroupDataAccess;
        this.categoryDataAccess = categoryDataAccess;
        this.projectDataAccess = projectDataAccess;
        this.correspondentDataAccess = correspondentDataAccess;
        this.templateDataAccess = templateDataAccess;
        this.transactionDataAccess = transactionDataAccess;
        this.currencyDataAccess = currencyDataAccess;
    }

    public void add(Account entity) {
        Guard.checkEntityForNull(entity);
        Guard.checkEntityNameForNull(entity);
        Guard.checkEntityForNull(entity.getParent());
        Guard.checkEntityForNull(entity.getCurrency());
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());

        AccountSubGroup subGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, entity.getParent().getId());
        Guard.checkEntityWithSameName(this.accountDataAccess, subGroup.getId(), entity);

        Account addedEntity = new Account();
        addedEntity.setId(entity.getId());
        addedEntity.setName(entity.getName());
        addedEntity.setDescription(entity.getDescription());
        addedEntity.setIsFavorite(entity.getIsFavorite());
        addedEntity.setOrder(this.accountDataAccess.getMaxOrder(subGroup.getId()) + 1);


        addedEntity.setCurrency(Guard.checkAndGetEntityById(this.currencyDataAccess, entity.getCurrency().getId()));

        if (entity.getCategory() != null) {
            addedEntity.setCategory(Guard.checkAndGetEntityById(this.categoryDataAccess, entity.getCategory().getId()));
        }
        if (entity.getProject() != null) {
            addedEntity.setProject(Guard.checkAndGetEntityById(this.projectDataAccess, entity.getProject().getId()));
        }
        if (entity.getCorrespondent() != null) {
            addedEntity.setCorrespondent(Guard.checkAndGetEntityById(this.correspondentDataAccess, entity.getCorrespondent().getId()));
        }

        subGroup.getChildren().add(addedEntity);
        addedEntity.setParent(subGroup);
        this.accountDataAccess.add(addedEntity);

        this.globalDataAccess.save();
    }

    public void update(Account entity) {
        Guard.checkEntityForNull(entity);
        Guard.checkEntityNameForNull(entity);

        Account updatedEntity = Guard.checkAndGetEntityById(this.accountDataAccess, entity.getId());
        this.accountDataAccess.loadParent(updatedEntity);
        AccountSubGroup subGroup = updatedEntity.getParent();

        Guard.checkEntityWithSameName(this.accountDataAccess, subGroup.getId(), entity);

        Category category = entity.getCategory() == null ? null : (Guard.checkAndGetEntityById(this.categoryDataAccess, entity.getCategory().getId()));
        Project project = entity.getProject() == null ? null : (Guard.checkAndGetEntityById(this.projectDataAccess, entity.getProject().getId()));
        Correspondent correspondent = entity.getCorrespondent() == null
                ? null : (Guard.checkAndGetEntityById(this.correspondentDataAccess, entity.getCorrespondent().getId()));

        updatedEntity.setName(entity.getName());
        updatedEntity.setDescription(entity.getDescription());
        updatedEntity.setIsFavorite(entity.getIsFavorite());

        updatedEntity.setCategory(category);
        updatedEntity.setProject(project);
        updatedEntity.setCorrespondent(correspondent);

        this.accountDataAccess.update(updatedEntity);

        this.globalDataAccess.save();
    }

    public void delete(UUID entityId) {
        Guard.checkObjectForNull(entityId, "entityId");
        Account account = Guard.checkAndGetEntityById(this.accountDataAccess, entityId);
        if (this.transactionDataAccess.getTransactionEntriesCount(account.getId()) > 0) {
            throw new IllegalArgumentException("Account cannot be delete, it contains transaction.");
        }
        if (this.templateDataAccess.getTemplateEntriesCount(account.getId()) > 0) {
            throw new IllegalArgumentException("Account cannot be delete, it contains template.");
        }

        this.accountDataAccess.loadParent(account);
        AccountSubGroup subGroup = account.getParent();
        this.accountSubGroupDataAccess.loadChildren(subGroup);

        subGroup.getChildren().remove(account);
        this.accountDataAccess.delete(account);

        OrderedEntityHelper.reorder(subGroup.getChildren());
        this.accountDataAccess.updateList(subGroup.getChildren());

        this.globalDataAccess.save();
    }

    public void setFavoriteStatus(UUID entityId, boolean isFavorite) {
        Guard.checkObjectForNull(entityId, "entityId");
        Account account = Guard.checkAndGetEntityById(this.accountDataAccess, entityId);
        if (account.getIsFavorite() != isFavorite) {
            account.setIsFavorite(isFavorite);
            this.accountDataAccess.update(account);
        }

        this.globalDataAccess.save();
    }

    public void setOrder(UUID entityId, int order) {
        Guard.checkObjectForNull(entityId, "entityId");
        Account account = Guard.checkAndGetEntityById(this.accountDataAccess, entityId);
        if (account.getOrder() != order) {
            this.accountDataAccess.loadParent(account);
            AccountSubGroup subGroup = account.getParent();
            this.accountSubGroupDataAccess.loadChildren(subGroup);

            OrderedEntityHelper.setOrder(subGroup.getChildren(), account, order);
            this.accountDataAccess.updateList(subGroup.getChildren());
        }

        this.globalDataAccess.save();
    }

    public void moveToAnotherParent(UUID entityId, UUID parentId) {
        Guard.checkObjectForNull(entityId, "entityId");
        Guard.checkObjectForNull(parentId, "parentId");
        Account account = Guard.checkAndGetEntityById(this.accountDataAccess, entityId);
        this.accountDataAccess.loadParent(account);
        AccountSubGroup fromAccountSubGroup = account.getParent();

        AccountSubGroup toAccountSubGroup = Guard.checkAndGetEntityById(this.accountSubGroupDataAccess, parentId);

        if (fromAccountSubGroup.getId() != toAccountSubGroup.getId()) {

            Guard.checkEntityWithSameName(this.accountDataAccess, toAccountSubGroup.getId(), account);

            this.accountSubGroupDataAccess.loadChildren(fromAccountSubGroup);

            fromAccountSubGroup.getChildren().remove(account);
            toAccountSubGroup.getChildren().add(account);
            account.setParent(toAccountSubGroup);
            account.setOrder(this.accountDataAccess.getMaxOrder(toAccountSubGroup.getId()) + 1);

            this.accountDataAccess.update(account);

            OrderedEntityHelper.reorder(fromAccountSubGroup.getChildren());
            this.accountDataAccess.updateList(fromAccountSubGroup.getChildren());
        }

        this.globalDataAccess.save();
    }

    public void combineTwoEntities(UUID primaryId, UUID secondaryId) {
        Guard.checkObjectForNull(primaryId, "primaryId");
        Guard.checkObjectForNull(secondaryId, "secondaryId");
        Account primaryAccount = Guard.checkAndGetEntityById(this.accountDataAccess, primaryId);
        Account secondaryAccount = Guard.checkAndGetEntityById(this.accountDataAccess, secondaryId);

        if (primaryAccount.getId() != secondaryAccount.getId()) {
            this.accountDataAccess.loadCurrency(primaryAccount);
            this.accountDataAccess.loadCurrency(secondaryAccount);
            if (primaryAccount.getCurrency().getId() != secondaryAccount.getCurrency().getId()) {
                throw new IllegalArgumentException("Can't combine accounts with different currencies");
            }

            ArrayList<TemplateEntry> templates = this.templateDataAccess.getEntriesByAccount(secondaryAccount);
            for (TemplateEntry templateEntry : templates) {
                templateEntry.setAccount(primaryAccount);
                this.templateDataAccess.update(templateEntry.getTemplate());
            }

            List<TransactionEntry> transactions = this.transactionDataAccess.getEntriesByAccount(secondaryAccount);
            for (TransactionEntry transactionEntry : transactions) {
                transactionEntry.setAccount(primaryAccount);
                this.transactionDataAccess.update(transactionEntry.getTransaction());
            }

            this.accountDataAccess.loadParent(secondaryAccount);
            AccountSubGroup secondarySubGroup = secondaryAccount.getParent();
            this.accountSubGroupDataAccess.loadChildren(secondarySubGroup);

            secondarySubGroup.getChildren().remove(secondaryAccount);
            this.accountDataAccess.delete(secondaryAccount);

            OrderedEntityHelper.reorder(secondarySubGroup.getChildren());
            this.accountDataAccess.updateList(secondarySubGroup.getChildren());
        }

        this.globalDataAccess.save();
    }
}
