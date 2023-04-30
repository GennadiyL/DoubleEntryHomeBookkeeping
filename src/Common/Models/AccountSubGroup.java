package Common.Models;

import Common.Models.Base.ReferenceParentEntity;
import Common.Models.Interfaces.IReferenceChildEntity;

public class AccountSubGroup extends ReferenceParentEntity<Account> implements IReferenceChildEntity<AccountGroup> {

    //region Parent
    private AccountGroup parent;
    @Override
    public AccountGroup getParent() {
        return this.parent;
    }
    @Override
    public void setParent(AccountGroup value) {
        this.parent = value;
    }
    //endregion
}
