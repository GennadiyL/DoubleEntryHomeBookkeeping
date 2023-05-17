package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbProjectGroupDataAccess
        extends MemoryDbReferenceParentEntityDataAccess<ProjectGroup>
        implements IProjectGroupDataAccess {
    public MemoryDbProjectGroupDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    protected ArrayList<ProjectGroup> getEntities()  {
        return this.getLedger().getProjectGroups();
    }
}
