package Common.Services;

import Common.Models.*;
import Common.Services.Base.*;

public interface ICurrencyService extends IOrderedEntityService, IFavoriteEntityService{
    void add(String isoCode);
    void delete(String isoCode);
}
