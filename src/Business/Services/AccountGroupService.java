package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

public class AccountGroupService extends ReferenceParentEntityService<AccountGroup, AccountSubGroup> implements IAccountGroupService {
    public AccountGroupService(
            IGlobalDataAccess globalDataAccess,
            IAccountGroupDataAccess entityDataAccess,
            IAccountSubGroupDataAccess childEntityDataAccess) {
        super(globalDataAccess, entityDataAccess, childEntityDataAccess);
    }
}
