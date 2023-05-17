package Common.Models;

import Common.Models.Base.ReferenceParentEntity;
import Common.Models.Interfaces.IReferenceChildEntity;

public class AccountSubGroup extends ReferenceParentEntity<Account> implements IReferenceChildEntity<AccountGroup> {

    private AccountGroup parent;

    public AccountGroup getParent() {
        return this.parent;
    }
    public void setParent(AccountGroup value) {
        this.parent = value;
    }
}
