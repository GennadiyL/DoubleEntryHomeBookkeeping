package Common.DataAccess.Base;

import Common.Models.Interfaces.IEntity;
import Common.Models.Interfaces.INamedEntity;

public interface IReferenceParentEntityDataAccess <T extends IEntity & INamedEntity>
        extends IEntityDataAccess<T>, IParentEntityDataAccess<T>{
}
