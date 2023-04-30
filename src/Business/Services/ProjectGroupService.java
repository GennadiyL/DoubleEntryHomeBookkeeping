package Business.Services;

import Business.Services.Base.*;
import Common.DataAccess.*;
import Common.Models.*;
import Common.Services.*;

public class ProjectGroupService extends ReferenceParentEntityService<ProjectGroup, Project> implements IProjectGroupService {
    public ProjectGroupService(
            IGlobalDataAccess globalDataAccess,
            IProjectGroupDataAccess entityDataAccess,
            IProjectDataAccess childEntityDataAccess) {
        super(globalDataAccess, entityDataAccess, childEntityDataAccess);
    }
}
