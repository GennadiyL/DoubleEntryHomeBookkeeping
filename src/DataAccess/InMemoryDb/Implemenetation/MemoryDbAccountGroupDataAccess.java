package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.IAccountGroupDataAccess;
import Common.Models.AccountGroup;

import java.util.ArrayList;
import java.util.UUID;

public class MemoryDbAccountGroupDataAccess implements IAccountGroupDataAccess {
    @Override
    public AccountGroup get(UUID id) {
        return null;
    }

    @Override
    public void add(AccountGroup entity) {

    }

    @Override
    public void addList(ArrayList<AccountGroup> list) {

    }

    @Override
    public void update(AccountGroup entity) {

    }

    @Override
    public void updateList(ArrayList<AccountGroup> list) {

    }

    @Override
    public void delete(AccountGroup entity) {

    }

    @Override
    public void deleteList(ArrayList<AccountGroup> list) {

    }

    @Override
    public ArrayList<AccountGroup> getByName(String name) {
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
    public ArrayList<AccountGroup> getList() {
        return null;
    }

    @Override
    public void loadChildren(AccountGroup entity) {

    }
}
