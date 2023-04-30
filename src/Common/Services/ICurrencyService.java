package Common.Services;

import Common.Models.*;
import Common.Services.Base.*;

public interface ICurrencyService extends IOrderedEntityService, IFavoriteEntityService{
    void add(Currency currency);
    void delete(Currency currency); //TODO: rework to ISO code
}
