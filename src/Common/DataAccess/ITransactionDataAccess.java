package Common.DataAccess;

import Common.DataAccess.Base.IEntityDataAccess;
import Common.Models.*;

import java.util.ArrayList;
import java.util.UUID;

public interface ITransactionDataAccess extends IEntityDataAccess<Transaction> {
    ArrayList<TransactionEntry> getEntriesByAccount(Account account);
    int getTransactionEntriesCount(UUID accountId);
}
