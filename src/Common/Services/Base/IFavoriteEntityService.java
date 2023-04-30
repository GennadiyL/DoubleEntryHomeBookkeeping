package Common.Services.Base;

import java.util.UUID;

public interface IFavoriteEntityService {
    void setFavoriteStatus(UUID entityId, boolean isFavorite);
}
