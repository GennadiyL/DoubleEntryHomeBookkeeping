package DataAccess.InMemoryDb.Implemenetation.Base;

import Common.DataAccess.Base.*;
import Common.Models.Interfaces.*;
import DataAccess.InMemoryDb.ILedgerFactory;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.UUID;
import java.util.stream.Collectors;
import java.util.stream.Stream;

public abstract class MemoryDbReferenceChildEntityDataAccess
        <T extends IEntity & ITrackedEntity & INamedEntity & IOrderedEntity & IChildEntity<TParent>, TParent extends IEntity>
        extends MemoryDbEntityDataAccess<T> implements IReferenceChildEntityDataAccess<T> {

    public MemoryDbReferenceChildEntityDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    //region Interface immplementation
    @Override
    public ArrayList<T> getByName(UUID parentId, String name) {
        return this.getEntitiesStream(parentId).filter(e -> e.getName().equalsIgnoreCase(name)).collect(Collectors.toCollection(ArrayList::new));
    }

    @Override
    public int getMaxOrder(UUID parentId) {
        return this.getEntitiesStream(parentId).max(Comparator.comparing(e -> e.getOrder())).get().getOrder();
    }

    @Override
    public int getCount(UUID parentId) {
        return (int) this.getEntitiesStream(parentId).count();
    }

    @Override
    public ArrayList<T> getList(UUID parentId) {
        return this.getEntitiesStream(parentId).collect(Collectors.toCollection(ArrayList::new));
    }

    @Override
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
