package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.*;

import java.util.*;

public interface ITransactionDataAccess
        extends IEntityDataAccess<Transaction> {
    ArrayList<TransactionEntry> getEntriesByAccount(Account account);
    int getTransactionEntriesCount(UUID accountId);
}
