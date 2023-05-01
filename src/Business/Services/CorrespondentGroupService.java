package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

public class CorrespondentGroupService extends ReferenceParentEntityService<CorrespondentGroup, Correspondent> implements ICorrespondentGroupService {
    public CorrespondentGroupService(
            IGlobalDataAccess globalDataAccess,
            ICorrespondentGroupDataAccess entityDataAccess,
            ICorrespondentDataAccess childEntityDataAccess) {
        super(globalDataAccess, entityDataAccess, childEntityDataAccess);
    }
}
