package Common.DataAccess;

import Common.Models.Interfaces.*;

import java.util.*;

public interface IGlobalDataAccess {
    void save();
    IEntity getEntity(UUID id);
}
