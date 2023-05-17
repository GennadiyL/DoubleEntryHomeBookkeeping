package Business.Services;

import Common.DataAccess.*;
import Common.Models.*;
import Common.Models.Enums.*;
import Common.Services.*;
import Business.Utils.*;
import Common.Utils.Tuples.Tuple2;

import java.math.BigDecimal;
import java.time.LocalDateTime;
import java.util.*;

public class TransactionService implements ITransactionService {
    private final ITransactionDataAccess transactionDataAccess;
    private final IAccountDataAccess accountDataAccess;
    private final ISystemConfigDataAccess systemConfigDataAccess;
    private final IGlobalDataAccess globalDataAccess;
    private final BigDecimal zero = new BigDecimal(0);
    private final BigDecimal one = new BigDecimal(1);

    public TransactionService(ITransactionDataAccess transactionDataAccess
            , IAccountDataAccess accountDataAccess
            , ISystemConfigDataAccess systemConfigDataAccess
            , IGlobalDataAccess globalDataAccess) {
        this.transactionDataAccess = transactionDataAccess;
        this.accountDataAccess = accountDataAccess;
        this.systemConfigDataAccess = systemConfigDataAccess;
        this.globalDataAccess = globalDataAccess;
    }

    public void add(Transaction entity) {
        this.globalDataAccess.load();

        checkInputTransactionEntity(entity);
        Guard.checkEntityWithSameId(this.globalDataAccess, entity.getId());

        Transaction addedTransaction = new Transaction();

        Tuple2<List<TransactionEntry>, BigDecimal> tuple = createEntries(entity, addedTransaction);
        List<TransactionEntry> addedEntries = tuple.getValue1();
        BigDecimal sumAmount = tuple.getValau2();

        addedTransaction.setComment(entity.getComment());
        addedTransaction.setDateTime(entity.getDateTime());
        addedTransaction.setState(sumAmount.compareTo(zero) == 0 ? entity.getState() : TransactionState.NoValid);
        addedTransaction.getEntries().addAll(addedEntries);

        this.transactionDataAccess.add(addedTransaction);

        this.globalDataAccess.save();
    }

    public void update(Transaction entity) {
        this.globalDataAccess.load();
        checkInputTransactionEntity(entity);
        Transaction updatedTransaction = Guard.checkAndGetEntityById(this.transactionDataAccess, entity.getId());

        Tuple2<List<TransactionEntry>, BigDecimal> tuple = createEntries(entity, updatedTransaction);
        List<TransactionEntry> addedEntries = tuple.getValue1();
        BigDecimal sumAmount = tuple.getValau2();

        updatedTransaction.setComment(entity.getComment());
        updatedTransaction.setDateTime(entity.getDateTime());
        updatedTransaction.setState(sumAmount.compareTo(zero) == 0 ? entity.getState() : TransactionState.NoValid);
        updatedTransaction.getEntries().clear();
        updatedTransaction.getEntries().addAll(addedEntries);
        this.transactionDataAccess.update(updatedTransaction);

        this.globalDataAccess.save();
    }

    public void delete(UUID entityId) {
        this.globalDataAccess.load();

        Transaction deletedTransaction = Guard.checkAndGetEntityById(this.transactionDataAccess, entityId);
        this.transactionDataAccess.delete(deletedTransaction);

        this.globalDataAccess.save();
    }

    @Override
    public void deleteTransactionList(ArrayList<UUID> transactionIds) {
        this.globalDataAccess.load();

        ArrayList<Transaction> deletedTransactions = new ArrayList<Transaction>();
        for (UUID transactionId : transactionIds) {
            Transaction deletedTransaction = Guard.checkAndGetEntityById(this.transactionDataAccess, transactionId);
            deletedTransactions.add(deletedTransaction);
        }
        this.transactionDataAccess.deleteList(deletedTransactions);

        this.globalDataAccess.save();
    }

    private void checkInputTransactionEntity(Transaction entity) {
        Guard.checkInputForNull(entity);

        LocalDateTime minDate = this.systemConfigDataAccess.getMinDate();
        if (entity.getDateTime().isBefore(minDate) || entity.getDateTime().isAfter(this.systemConfigDataAccess.getMaxDate())) {
            throw new IllegalArgumentException("Data and Time is not in valid range");
        }
        if (entity.getEntries() == null || entity.getEntries().size() < 2) {
            throw new IllegalArgumentException("Invalid amount of Transaction Entries: amount must be more than 1");
        }
    }

    private Tuple2<List<TransactionEntry>, BigDecimal> createEntries(Transaction entity, Transaction transaction) {
        String mainCurrencyIsoCode = this.systemConfigDataAccess.getMainCurrencyIsoCode();
        BigDecimal sumAmount = new BigDecimal(0);

        ArrayList<TransactionEntry> entries = new ArrayList<TransactionEntry>();
        for (TransactionEntry entry : entity.getEntries()) {
            if (entry.getRate().compareTo(zero) <= 0) {
                throw new IllegalArgumentException("Currency rate must be more than 0");
            }
            Guard.checkInputForNull(entry.getAccount());
            Account account = Guard.checkAndGetEntityById(this.accountDataAccess, entry.getAccount().getId());
            this.accountDataAccess.loadCurrency(account);

            if (account.getCurrency().getIsoCode() == mainCurrencyIsoCode && entry.getRate().compareTo(one) != 0) {
                throw new IllegalArgumentException("Rate for main Currency should be 1");
            }

            TransactionEntry addedEntry = new TransactionEntry();
            addedEntry.setAccount(account);
            addedEntry.setRate(entry.getRate());
            addedEntry.setAmount(entry.getAmount());
            addedEntry.setTransaction(transaction);
            entries.add(addedEntry);

            sumAmount = sumAmount.add(addedEntry.getAmount().multiply(addedEntry.getRate()));
        }
        return new Tuple2<>(entries, sumAmount);
    }
}
