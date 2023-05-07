package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbCategoryDataAccess extends MemoryDbReferenceChildEntityDataAccess<Category, CategoryGroup> implements ICategoryDataAccess {
    public MemoryDbCategoryDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    @Override
    protected ArrayList getEntities() {
        return this.getLedger().getCategories();
    }
}
