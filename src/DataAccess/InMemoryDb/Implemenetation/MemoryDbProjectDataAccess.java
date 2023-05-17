package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbProjectDataAccess extends MemoryDbReferenceChildEntityDataAccess<Project, ProjectGroup> implements IProjectDataAccess{
    public MemoryDbProjectDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    protected ArrayList<Project> getEntities() {
        return this.getLedger().getProjects();
    }
}
