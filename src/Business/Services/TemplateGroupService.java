package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

public class TemplateGroupService extends ReferenceParentEntityService<TemplateGroup, Template> implements ITemplateGroupService {
    public TemplateGroupService(
            IGlobalDataAccess globalDataAccess,
            ITemplateGroupDataAccess entityDataAccess,
            ITemplateDataAccess childEntityDataAccess) {
        super(globalDataAccess, entityDataAccess, childEntityDataAccess);
    }
}
