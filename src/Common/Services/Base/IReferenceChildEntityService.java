package Common.Services.Base;

import Common.Models.Interfaces.IEntity;

public interface IReferenceChildEntityService<T extends IEntity> extends IReferenceEntityService<T>, IChildEntityService, ICombinedEntityService {
}
