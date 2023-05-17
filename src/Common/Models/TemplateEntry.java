package Common.Models;

import Common.Models.Base.Entity;

import java.math.BigDecimal;

public class TemplateEntry extends Entity {

    private Template template;
    private Account account;
    private BigDecimal amount;

    public Template getTemplate() {
        return this.template;
    }
    public void setTemplate(Template template) {
        this.template = template;
    }

    public Account getAccount() {
        return this.account;
    }
    public void setAccount(Account account) {
        this.account = account;
    }

    public BigDecimal getAmount() {
        return this.amount;
    }
    public void setAmount(BigDecimal amount) {
        this.amount = amount;
    }
}
