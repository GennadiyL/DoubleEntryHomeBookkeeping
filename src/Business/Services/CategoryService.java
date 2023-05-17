package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

import java.util.*;

public class CategoryService extends ReferenceChildEntityService<Category, CategoryGroup> implements ICategoryService {
    public CategoryService(
            IGlobalDataAccess globalDataAccess,
            ICategoryDataAccess entityDataAccess,
            ICategoryGroupDataAccess parentEntityDataAccess,
            IAccountDataAccess accountDataAccess) {
        super(globalDataAccess, entityDataAccess, parentEntityDataAccess, accountDataAccess);
    }

    
    protected ArrayList<Account> GetAccountsByEntity(Category entity) {
        return this.getAccountDataAccess().getAccountsByCategory(entity);
    }
    
    protected void AccountEntitySetter(Category entity, Account account) {
        account.setCategory(entity);
    }
}
