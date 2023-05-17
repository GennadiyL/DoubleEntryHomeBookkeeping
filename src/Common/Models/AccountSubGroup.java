package Common.Models;

import Common.Models.Base.*;
import Common.Models.Interfaces.*;

public class AccountSubGroup extends ReferenceParentEntity<Account> implements IReferenceChildEntity<AccountGroup> {

    private AccountGroup parent;

    public AccountGroup getParent() {
        return this.parent;
    }
    public void setParent(AccountGroup value) {
        this.parent = value;
    }
}
