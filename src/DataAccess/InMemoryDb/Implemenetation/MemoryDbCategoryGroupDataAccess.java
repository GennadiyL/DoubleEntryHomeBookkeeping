package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.ICategoryGroupDataAccess;
import Common.Models.CategoryGroup;
import DataAccess.InMemoryDb.ILedgerFactory;
import DataAccess.InMemoryDb.Implemenetation.Base.MemoryDbReferenceParentEntityDataAccess;

import java.util.*;

public class MemoryDbCategoryGroupDataAccess extends MemoryDbReferenceParentEntityDataAccess<CategoryGroup> implements ICategoryGroupDataAccess {


    public MemoryDbCategoryGroupDataAccess(ILedgerFactory ledgerFactory, ILedgerFactory factory) {
        super(ledgerFactory);
    }

    @Override
    protected ArrayList<CategoryGroup> getEntities() {
        return this.getLedger().getCategoryGroups();
    }
}
