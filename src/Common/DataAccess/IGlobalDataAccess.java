package Common.DataAccess;

import Common.Models.Interfaces.*;

import java.util.UUID;

public interface IGlobalDataAccess {
    void load();
    void save();
    IEntity get(UUID id);
}
