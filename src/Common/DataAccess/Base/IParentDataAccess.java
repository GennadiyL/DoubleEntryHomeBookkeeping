package Common.DataAccess.Base;

import Common.Models.Interfaces.*;

public interface IParentDataAccess<T extends IEntity>  {
    void loadChildren(T entity);
}
