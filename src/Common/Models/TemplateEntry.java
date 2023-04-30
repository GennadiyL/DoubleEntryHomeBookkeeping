package Common.Models;

import Common.Models.Base.Entity;

import java.math.BigDecimal;

public class TemplateEntry extends Entity {

    //region Template
    Template template;
    public Template getTemplate() {
        return this.template;
    }
    public void setTemplate(Template template) {
        this.template = template;
    }
    //endregion

    //region Account
    Account account;
    public Account getAccount() {
        return this.account;
    }
    public void setAccount(Account account) {
        this.account = account;
    }
    //endregion

    //region Amount
    BigDecimal amount;
    public BigDecimal getAmount() {
        return this.amount;
    }
    public void setAmount(BigDecimal amount) {
        this.amount = amount;
    }
    //endregion
}
