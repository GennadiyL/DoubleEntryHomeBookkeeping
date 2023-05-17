package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbAccountSubGroupDataAccess
        extends MemoryDbReferenceChildEntityDataAccess<AccountSubGroup, AccountGroup>
        implements IAccountSubGroupDataAccess {
    public MemoryDbAccountSubGroupDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    @Override
    public void loadChildren(AccountSubGroup entity) {
        //Only for Relational Db
    }

    @Override
    protected ArrayList<AccountSubGroup> getEntities() {
        return this.getLedger().getAccountSubGroups();
    }
}
