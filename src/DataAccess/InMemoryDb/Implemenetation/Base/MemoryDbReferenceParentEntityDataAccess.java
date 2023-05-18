package DataAccess.InMemoryDb.Implemenetation.Base;

import Common.DataAccess.Base.*;
import Common.Models.Currency;
import Common.Models.Interfaces.*;
import DataAccess.InMemoryDb.*;

import java.util.*;
import java.util.stream.*;

public abstract class MemoryDbReferenceParentEntityDataAccess<T extends IEntity & ITrackedEntity & INamedEntity & IOrderedEntity>
        extends MemoryDbEntityDataAccess<T>
        implements IReferenceParentEntityDataAccess<T> {

    public MemoryDbReferenceParentEntityDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    //region Interface Implementation
    public ArrayList<T> getByName(String name) {
        return this.getEntitiesStream().filter(g -> g.getName().equalsIgnoreCase(name)).collect(Collectors.toCollection(ArrayList::new));
    }

    public int getMaxOrder() {
        Optional<T> t = this.getEntitiesStream().max(Comparator.comparing(IOrderedEntity::getOrder));
        if(t.isPresent()){
            return t.get().getOrder();
        }
        return 0;
    }

    public int getCount() {
        return this.getEntities().size();
    }

    public ArrayList<T> getList() {
        return this.getEntities();
    }

    public void loadChildren(T entity) {
        //Only for Relational Db
    }
    //endregion
}
