package Common.DataAccess;

import Common.Models.Interfaces.*;

import java.util.UUID;

public interface IGlobalDataAccess {
    void Load();
    void Save();
    IEntity Get(UUID id);
}
