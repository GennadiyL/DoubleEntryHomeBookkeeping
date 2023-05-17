package DataAccess.InMemoryDb.Implemenetation.Base;

import Common.DataAccess.Base.*;
import Common.Models.Interfaces.*;
import DataAccess.InMemoryDb.*;

import java.util.*;
import java.util.stream.*;

public abstract class MemoryDbReferenceChildEntityDataAccess
        <T extends IEntity & ITrackedEntity & INamedEntity & IOrderedEntity & IChildEntity<TParent>, TParent extends IEntity>
        extends MemoryDbEntityDataAccess<T>
        implements IReferenceChildEntityDataAccess<T> {

    public MemoryDbReferenceChildEntityDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    //region Interface implementation
    public ArrayList<T> getByName(UUID parentId, String name) {
        return this.getEntitiesStream(parentId).filter(e -> e.getName().equalsIgnoreCase(name)).collect(Collectors.toCollection(ArrayList::new));
    }

    public int getMaxOrder(UUID parentId) {
        return this.getEntitiesStream(parentId).max(Comparator.comparing(e -> e.getOrder())).get().getOrder();
    }

    public int getCount(UUID parentId) {
        return (int) this.getEntitiesStream(parentId).count();
    }

    public ArrayList<T> getList(UUID parentId) {
        return this.getEntitiesStream(parentId).collect(Collectors.toCollection(ArrayList::new));
    }

    public void loadParent(T entity) {
        //Only for Relational Db
    }
    //endregion

    //region Private Members
    protected Stream<T> getEntitiesStream(UUID parentId) {
        return this.getEntitiesStream().filter(e -> e.getParent().getId() == parentId);
    }
    //endregion
}
