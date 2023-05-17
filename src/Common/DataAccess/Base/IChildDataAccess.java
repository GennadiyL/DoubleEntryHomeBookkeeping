package Common.DataAccess.Base;

import Common.Models.Interfaces.*;

public interface IChildDataAccess<T extends IEntity>  {
    void loadParent(T entity);
}
