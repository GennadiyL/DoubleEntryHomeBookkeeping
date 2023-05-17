package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;
import java.util.stream.*;

public class MemoryDbTransactionDataAccess
        extends MemoryDbEntityDataAccess<Transaction>
        implements ITransactionDataAccess {

    public MemoryDbTransactionDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    public ArrayList<TransactionEntry> getEntriesByAccount(Account account) {
        Stream<TransactionEntry> entryStream;
        if (account != null) {
            entryStream = getNotNullAccountEntryStream(account.getId());
        }
        else {
            entryStream = getNullAccountEntryStream();
        }
        return entryStream.collect(Collectors.toCollection(ArrayList::new));
    }

    public int getTransactionEntriesCount(UUID accountId) {
        return (int) getNotNullAccountEntryStream(accountId).count();
    }

    protected ArrayList<Transaction> getEntities() {
        return this.getLedger().getTransactions();
    }

    private Stream<TransactionEntry> getNotNullAccountEntryStream(UUID accountId) {
        return this.getEntitiesStream()
                .flatMap(e1 -> e1.getEntries().stream())
                .filter(e -> e.getAccount() != null && e.getAccount().getId() == accountId);
    }

    private Stream<TransactionEntry> getNullAccountEntryStream() {
        return this.getEntitiesStream()
                .flatMap(e1 -> e1.getEntries().stream())
                .filter(e -> e.getAccount() == null);
    }

}
