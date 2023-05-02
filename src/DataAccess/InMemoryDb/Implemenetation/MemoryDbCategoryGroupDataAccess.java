package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.ICategoryGroupDataAccess;
import Common.Models.CategoryGroup;
import Common.Utils.DateTimeUtils;
import DataAccess.InMemoryDb.ILedgerFactory;
import DataAccess.InMemoryDb.Ledger;

import java.util.ArrayList;
import java.util.UUID;

public class MemoryDbCategoryGroupDataAccess implements ICategoryGroupDataAccess {

    private final ILedgerFactory factory;

    public MemoryDbCategoryGroupDataAccess(ILedgerFactory ledgerFactory) {
        this.factory = ledgerFactory;
    }

    @Override
    public CategoryGroup get(UUID id) {
        Ledger ledger = factory.get();
        return ledger.getCategoryGroups().stream().filter(g -> g.getId().equals(id)).findFirst().orElse(null);
    }

    @Override
    public void add(CategoryGroup entity) {
        Ledger ledger = factory.get();
        entity.setTimeStamp(DateTimeUtils.getDateTimeStamp());
        ledger.getCategoryGroups().add(entity);
        ledger.getAddedIds().add(entity.getId());
    }

    @Override
    public void addList(ArrayList<CategoryGroup> list) {
        Ledger ledger = factory.get();
        String dateTimeStamp = DateTimeUtils.getDateTimeStamp();
        list.forEach(e -> {
            e.setTimeStamp(dateTimeStamp);
            ledger.getCategoryGroups().add(e);
            ledger.getAddedIds().add(e.getId());
        });
    }

    @Override
    public void update(CategoryGroup entity) {
        Ledger ledger = factory.get();
        String dateTimeStamp = DateTimeUtils.getDateTimeStamp();
        entity.setTimeStamp(dateTimeStamp);
        ledger.getUpdatedIds().add(entity.getId());
    }

    @Override
    public void updateList(ArrayList<CategoryGroup> list) {
        Ledger ledger = factory.get();
        String dateTimeStamp = DateTimeUtils.getDateTimeStamp();
        list.forEach(e -> {
            e.setTimeStamp(dateTimeStamp);
            ledger.getUpdatedIds().add(e.getId());
        });
    }

    @Override
    public void delete(CategoryGroup entity) {
        Ledger ledger = factory.get();
        ledger.getCategoryGroups().remove(entity);
        ledger.getDeletedIds().add(entity.getId());
    }

    @Override
    public void deleteList(ArrayList<CategoryGroup> list) {
        Ledger ledger = factory.get();
        list.forEach(e -> {
            ledger.getCategoryGroups().remove(e);
            ledger.getDeletedIds().add(e.getId());
        });
    }

    @Override
    public ArrayList<CategoryGroup> getByName(String name) {
        return null;
    }

    @Override
    public int getMaxOrder() {
        return 0;
    }

    @Override
    public int getCount() {
        return 0;
    }

    @Override
    public ArrayList<CategoryGroup> getList() {
        return null;
    }

    @Override
    public void loadChildren(CategoryGroup entity) {

    }
}
