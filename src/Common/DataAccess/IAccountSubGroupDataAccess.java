package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.*;

public interface IAccountSubGroupDataAccess
        extends IReferenceParentEntityDataAccess<AccountSubGroup, Account>
        , IReferenceChildEntityDataAccess<AccountSubGroup, AccountGroup>
{
}
