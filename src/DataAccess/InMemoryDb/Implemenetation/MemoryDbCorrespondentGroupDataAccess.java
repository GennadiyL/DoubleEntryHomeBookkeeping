package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbCorrespondentGroupDataAccess
        extends MemoryDbReferenceParentEntityDataAccess<CorrespondentGroup>
        implements ICorrespondentGroupDataAccess {
    public MemoryDbCorrespondentGroupDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    protected ArrayList<CorrespondentGroup> getEntities() {
        return this.getLedger().getCorrespondentGroups();
    }
}
