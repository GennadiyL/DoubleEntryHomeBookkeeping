package Common.DataAccess.Base;

import Common.Models.Interfaces.*;

public interface IReferenceParentEntityDataAccess <T extends IEntity & INamedEntity>
        extends IEntityDataAccess<T>, IParentEntityDataAccess<T>{
}
