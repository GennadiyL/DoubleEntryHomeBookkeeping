package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

import java.util.*;

public class ProjectService extends ReferenceChildEntityService<Project, ProjectGroup> implements IProjectService {
    public ProjectService(
            IGlobalDataAccess globalDataAccess,
            IProjectDataAccess entityDataAccess,
            IProjectGroupDataAccess parentEntityDataAccess,
            IAccountDataAccess accountDataAccess) {
        super(globalDataAccess, entityDataAccess, parentEntityDataAccess, accountDataAccess);
    }

    @Override
    protected ArrayList<Account> GetAccountsByEntity(Project entity) {
        return this.getAccountDataAccess().GetAccountsByProject(entity);
    }

    @Override
    protected void AccountEntitySetter(Project entity, Account account) {
        account.setProject(entity);
    }
}
