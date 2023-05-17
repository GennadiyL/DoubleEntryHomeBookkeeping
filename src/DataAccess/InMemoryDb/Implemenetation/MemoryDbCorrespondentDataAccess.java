package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;

public class MemoryDbCorrespondentDataAccess
        extends MemoryDbReferenceChildEntityDataAccess<Correspondent, CorrespondentGroup>
        implements ICorrespondentDataAccess {
    public MemoryDbCorrespondentDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    protected ArrayList<Correspondent> getEntities() {
        return this.getLedger().getCorrespondents();
    }
}
