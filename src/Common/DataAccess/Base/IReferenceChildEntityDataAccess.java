package Common.DataAccess.Base;

import Common.Models.Interfaces.*;

public interface IReferenceChildEntityDataAccess<T extends IEntity & INamedEntity>
        extends IEntityDataAccess<T>, IChildEntityDataAccess<T>  {
}
