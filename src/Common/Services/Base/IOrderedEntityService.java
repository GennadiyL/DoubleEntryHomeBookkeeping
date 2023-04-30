package Common.Services.Base;

import java.util.UUID;

public interface IOrderedEntityService {
    void setOrder(UUID entityId, int order);
}
