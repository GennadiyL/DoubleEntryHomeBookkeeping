package DataAccess.InMemoryDb.Implemenetation;

import Common.DataAccess.*;
import Common.Models.*;
import DataAccess.InMemoryDb.*;
import DataAccess.InMemoryDb.Implemenetation.Base.*;

import java.util.*;
import java.util.stream.*;

public class MemoryDbTemplateDataAccess
        extends MemoryDbReferenceChildEntityDataAccess<Template, TemplateGroup>
        implements ITemplateDataAccess {
    public MemoryDbTemplateDataAccess(ILedgerFactory ledgerFactory) {
        super(ledgerFactory);
    }

    public ArrayList<TemplateEntry> getEntriesByAccount(Account account) {

        Stream<TemplateEntry> entryStream;
        if (account != null) {
            entryStream = getNotNullAccountEntryStream(account.getId());
        }
        else {
            entryStream = getNullAccountEntryStream();
        }
        return entryStream.collect(Collectors.toCollection(ArrayList::new));
    }

    public int getTemplateEntriesCount(UUID accountId) {
        return (int) getNotNullAccountEntryStream(accountId).count();
    }

    protected ArrayList<Template> getEntities() {
        return this.getLedger().getTemplates();
    }

    private Stream<TemplateEntry> getNotNullAccountEntryStream(UUID accountId) {
        return this.getEntitiesStream()
                .flatMap(e1 -> e1.getEntries().stream())
                .filter(e -> e.getAccount() != null && e.getAccount().getId() == accountId);
    }

    private Stream<TemplateEntry> getNullAccountEntryStream() {
        return this.getEntitiesStream()
                .flatMap(e1 -> e1.getEntries().stream())
                .filter(e -> e.getAccount() == null);
    }
}
