package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.IAccountDataAccess;
import Common.Models.Account;
import Common.Models.Category;
import Common.Models.Correspondent;
import Common.Models.Project;

import java.util.ArrayList;
import java.util.UUID;

public class MemoryDbAccountDataAccess implements IAccountDataAccess {

    @Override
    public ArrayList<Account> getByName(UUID parentId, String name) {
        return null;
    }

    @Override
    public int getMaxOrder(UUID parentId) {
        return 0;
    }

    @Override
    public int getCount(UUID parentId) {
        return 0;
    }

    @Override
    public ArrayList<Account> getList(UUID parentId) {
        return null;
    }

    @Override
    public void loadParent(Account entity) {

    }

    @Override
    public Account get(UUID id) {
        return null;
    }

    @Override
    public void add(Account entity) {

    }

    @Override
    public void addList(ArrayList<Account> list) {

    }

    @Override
    public void update(Account entity) {

    }

    @Override
    public void updateList(ArrayList<Account> list) {

    }

    @Override
    public void delete(Account entity) {

    }

    @Override
    public void deleteList(ArrayList<Account> list) {

    }

    @Override
    public void loadCurrency(Account account) {

    }

    @Override
    public ArrayList<Account> getAccountsByCorrespondent(Correspondent correspondent) {
        return null;
    }

    @Override
    public ArrayList<Account> getAccountsByCategory(Category category) {
        return null;
    }

    @Override
    public ArrayList<Account> getAccountsByProject(Project project) {
        return null;
    }
}
