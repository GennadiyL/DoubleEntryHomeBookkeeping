package Common.Services.Base;

import Common.Models.Interfaces.IEntity;

import java.util.UUID;

public interface IEntityService<T extends IEntity> {
    void add(T entity);
    void update(T entity);
    void delete(UUID entityId);
}
