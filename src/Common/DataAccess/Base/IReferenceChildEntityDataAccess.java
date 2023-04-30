package Common.DataAccess.Base;

import Common.Models.Interfaces.IEntity;
import Common.Models.Interfaces.INamedEntity;

public interface IReferenceChildEntityDataAccess<T extends IEntity & INamedEntity, TParent extends IEntity>
        extends IEntityDataAccess<T>, IChildEntityDataAccess<T, TParent>  {
}
