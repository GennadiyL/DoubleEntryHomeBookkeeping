package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;
import java.util.stream.*;

public class MemoryDbAccountDataAccess extends MemoryDbReferenceChildEntityDataAccess<Account, AccountSubGroup> implements IAccountDataAccess {
    public MemoryDbAccountDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    @Override
    public void loadCurrency(Account account) {
        //Only for Relational Db
    }

    @Override
    public ArrayList<Account> getAccountsByCorrespondent(Correspondent correspondent) {
        return this.getEntitiesStream().filter(e -> e.getCorrespondent().getId() == correspondent.getId()).collect(Collectors.toCollection(ArrayList::new));
    }

    @Override
    public ArrayList<Account> getAccountsByCategory(Category category) {
        return this.getEntitiesStream().filter(e -> e.getCategory().getId() == category.getId()).collect(Collectors.toCollection(ArrayList::new));
    }

    @Override
    public ArrayList<Account> getAccountsByProject(Project project) {
        return this.getEntitiesStream().filter(e -> e.getProject().getId() == project.getId()).collect(Collectors.toCollection(ArrayList::new));
    }

    @Override
    protected ArrayList<Account> getEntities() {
        return this.getLedger().getAccounts();
    }
}
