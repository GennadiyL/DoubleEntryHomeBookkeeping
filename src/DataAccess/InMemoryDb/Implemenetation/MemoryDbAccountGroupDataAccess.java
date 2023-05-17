package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbAccountGroupDataAccess
        extends MemoryDbReferenceParentEntityDataAccess<AccountGroup>
        implements IAccountGroupDataAccess {

    public MemoryDbAccountGroupDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    protected ArrayList<AccountGroup> getEntities() {
         return this.getLedger().getAccountGroups();
    }
}
