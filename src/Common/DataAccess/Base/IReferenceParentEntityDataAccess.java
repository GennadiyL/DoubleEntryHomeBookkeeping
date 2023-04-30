package Common.DataAccess.Base;

import Common.Models.Interfaces.IEntity;
import Common.Models.Interfaces.INamedEntity;

public interface IReferenceParentEntityDataAccess <T extends IEntity & INamedEntity, TChild extends IEntity>
        extends IEntityDataAccess<T>, IParentEntityDataAccess<T, TChild>{
}
