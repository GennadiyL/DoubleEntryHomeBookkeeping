package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbTemplateGroupDataAccess  extends MemoryDbReferenceParentEntityDataAccess<TemplateGroup> implements ITemplateGroupDataAccess {
    public MemoryDbTemplateGroupDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    @Override
    protected ArrayList<TemplateGroup> getEntities()  {
        return this.getLedger().getTemplateGroups();
    }
}
