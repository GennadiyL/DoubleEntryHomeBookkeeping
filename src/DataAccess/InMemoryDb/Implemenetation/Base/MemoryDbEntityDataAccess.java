package DataAccess.InMemoryDb.Implemenetation.Base;

import Common.DataAccess.Base.*;
import Common.Models.Interfaces.*;
import Common.Utils.TrackedEntity.TimeStampHelper;
import DataAccess.InMemoryDb.*;
import org.jetbrains.annotations.NotNull;

import java.util.*;
import java.util.stream.Stream;

public abstract class MemoryDbEntityDataAccess<T extends IEntity & ITrackedEntity> implements IEntityDataAccess<T> {
    private final ILedgerFactory factory;

    public MemoryDbEntityDataAccess(ILedgerFactory ledgerFactory) {
        this.factory = ledgerFactory;
    }

    //region Interface Implementation
    @Override
    public T get( UUID id) {
        return this.getEntitiesStream().filter(g -> g.getId().equals(id)).findFirst().orElse(null);
    }

    @Override
    public void add( T entity) {
        entity.setTimeStamp(TimeStampHelper.getDateTimeStamp());
        this.getEntities().add(entity);
        this.getAddedIds().add(entity.getId());
    }

    @Override
    public void addList( ArrayList<T> list) {
        String dateTimeStamp = TimeStampHelper.getDateTimeStamp();
        list.forEach(e -> {
            e.setTimeStamp(dateTimeStamp);
            this.getEntities().add(e);
            this.getAddedIds().add(e.getId());
        });
    }

    @Override
    public void update( T entity) {
        entity.setTimeStamp(TimeStampHelper.getDateTimeStamp());
        this.getUpdatedIds().add(entity.getId());
    }

    @Override
    public void updateList( ArrayList<T> list) {
        String dateTimeStamp = TimeStampHelper.getDateTimeStamp();
        list.forEach(e -> {
            e.setTimeStamp(dateTimeStamp);
            this.getUpdatedIds().add(e.getId());
        });
    }

    @Override
    public void delete( T entity) {
        this.getEntities().remove(entity);
        this.getDeletedIds().add(entity.getId());
    }

    @Override
    public void deleteList( ArrayList<T> list) {
        list.forEach(e -> {
            this.getEntities().remove(e);
            this.getDeletedIds().add(e.getId());
        });
    }
    //endregion

    //region Protected Virtual
    protected abstract ArrayList<T> getEntities();
    //endregion

    //region Protected non-virtual
    protected Ledger getLedger() {
        return factory.get();
    }

    protected Stream<T> getEntitiesStream() {
        return this.getEntities().stream();
    }

    protected ArrayList<UUID> getAddedIds() {
        Ledger ledger = factory.get();
        return ledger.getAddedIds();
    }

    protected ArrayList<UUID> getUpdatedIds() {
        Ledger ledger = factory.get();
        return ledger.getUpdatedIds();
    }

    protected ArrayList<UUID> getDeletedIds() {
        Ledger ledger = factory.get();
        return ledger.getDeletedIds();
    }
    //endregion
}
