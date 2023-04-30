package Common.Services;

import Common.Models.*;
import Common.Services.Base.*;

import java.util.ArrayList;
import java.util.UUID;

public interface ITransactionService extends IEntityService<Transaction> {
    void deleteTransactionList(ArrayList<UUID> transactionIds);
}
