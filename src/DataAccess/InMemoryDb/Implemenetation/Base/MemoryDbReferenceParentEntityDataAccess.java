package DataAccess.InMemoryDb.Implemenetation.Base;

import Common.DataAccess.Base.IReferenceParentEntityDataAccess;
import Common.Models.Interfaces.*;
import DataAccess.InMemoryDb.*;

import java.util.ArrayList;
import java.util.Comparator;
import java.util.stream.Collectors;

public abstract class MemoryDbReferenceParentEntityDataAccess<T extends IEntity & ITrackedEntity & INamedEntity & IOrderedEntity>
        extends MemoryDbEntityDataAccess<T> implements IReferenceParentEntityDataAccess<T> {

    public MemoryDbReferenceParentEntityDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    //region Interface Implementation
    @Override
    public ArrayList<T> getByName(String name) {
        return this.getEntitiesStream().filter(g -> g.getName().equalsIgnoreCase(name)).collect(Collectors.toCollection(ArrayList::new));
    }

    @Override
    public int getMaxOrder() {
        return this.getEntitiesStream().max(Comparator.comparing(e -> e.getOrder())).get().getOrder();
    }

    @Override
    public int getCount() {
        return this.getEntities().size();
    }

    @Override
    public ArrayList<T> getList() {
        return this.getEntities();
    }

    @Override
    public void loadChildren(T entity) {
        //Only for Relational Db
    }
    //endregion
}
