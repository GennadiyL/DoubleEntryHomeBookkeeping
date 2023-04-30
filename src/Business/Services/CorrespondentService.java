package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

import java.util.*;

public class CorrespondentService extends ReferenceChildEntityService<Correspondent, CorrespondentGroup> implements ICorrespondentService {
    public CorrespondentService(
            IGlobalDataAccess globalDataAccess,
            ICorrespondentDataAccess entityDataAccess,
            ICorrespondentGroupDataAccess parentEntityDataAccess,
            IAccountDataAccess accountDataAccess) {
        super(globalDataAccess, entityDataAccess, parentEntityDataAccess, accountDataAccess);
    }

    @Override
    protected ArrayList<Account> GetAccountsByEntity(Correspondent entity) {
        return this.getAccountDataAccess().GetAccountsByCorrespondent(entity);
    }

    @Override
    protected void AccountEntitySetter(Correspondent entity, Account account) {
        account.setCorrespondent(entity);
    }
}
