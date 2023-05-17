package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbCategoryGroupDataAccess
        extends MemoryDbReferenceParentEntityDataAccess<CategoryGroup>
        implements ICategoryGroupDataAccess {

    public MemoryDbCategoryGroupDataAccess(ILedgerFactory ledgerFactory, ILedgerFactory factory) {
        super(ledgerFactory);
    }

    protected ArrayList<CategoryGroup> getEntities() {
        return this.getLedger().getCategoryGroups();
    }
}
