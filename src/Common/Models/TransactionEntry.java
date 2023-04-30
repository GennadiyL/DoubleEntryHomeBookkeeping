package Common.Models;

import Common.Models.Base.Entity;

import java.math.BigDecimal;

public class TransactionEntry extends Entity {

    //region Transaction
    Transaction transaction;

    public Transaction getTransaction() {
        return this.transaction;
    }

    public void setTransaction(Transaction transaction) {
        this.transaction = transaction;
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

    //region Rate
    BigDecimal rate;

    public BigDecimal getRate() {
        return this.rate;
    }

    public void setRate(BigDecimal rate) {
        this.rate = rate;
    }
    //endregion
}
