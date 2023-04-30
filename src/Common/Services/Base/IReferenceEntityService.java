package Common.Services.Base;

import Common.Models.Interfaces.IEntity;

public interface IReferenceEntityService<T extends IEntity> extends IEntityService<T>, IOrderedEntityService, IFavoriteEntityService {
}
