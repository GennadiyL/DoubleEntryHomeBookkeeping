package Common.Services.Base;

import java.util.UUID;

public interface ICombinedEntityService {
    void combineTwoEntities(UUID primaryId, UUID secondaryId);
}
