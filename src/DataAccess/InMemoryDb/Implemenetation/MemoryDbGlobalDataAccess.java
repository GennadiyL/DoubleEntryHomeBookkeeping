package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.Interfaces.*;
import DataAccess.InMemoryDb.*;

import java.util.*;

public class MemoryDbGlobalDataAccess
        implements IGlobalDataAccess {
    private final ILedgerFactory factory;

    public MemoryDbGlobalDataAccess(ILedgerFactory factory) {
        this.factory = factory;
    }

    public void save() {
        this.factory.send();
    }

    public IEntity getEntity(UUID id) {
        Ledger ledger = this.factory.get();
        ArrayList<ArrayList<? extends IEntity>> lists = ledger.getAllLists();
        for (ArrayList<? extends IEntity> list : lists) {
            IEntity entity = this.findInCollection(list, id);
            if(entity != null)
            {
                return entity;
            }
        }
        return null;
    }

    private <T extends  IEntity> T findInCollection(ArrayList<T> list, UUID id)
    {
        return list.stream().filter(e -> e.getId() == id).findFirst().orElse(null);
    }
}
