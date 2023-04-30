package Common.Services.Base;

import java.util.UUID;

public interface IChildEntityService {
    void moveToAnotherParent(UUID entityId, UUID parentId);
}
