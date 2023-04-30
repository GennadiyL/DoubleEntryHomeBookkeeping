package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

public class CategoryGroupService extends ReferenceParentEntityService<CategoryGroup, Category> implements ICategoryGroupService {
    public CategoryGroupService(
            IGlobalDataAccess globalDataAccess,
            ICategoryGroupDataAccess entityDataAccess,
            ICategoryDataAccess childEntityDataAccess) {
        super(globalDataAccess, entityDataAccess, childEntityDataAccess);
    }
}
