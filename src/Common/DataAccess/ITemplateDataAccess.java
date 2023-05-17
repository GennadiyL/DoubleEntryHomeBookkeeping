package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.*;

import java.util.*;

public interface ITemplateDataAccess
        extends IReferenceChildEntityDataAccess<Template>{
    ArrayList<TemplateEntry> getEntriesByAccount(Account account);
    int getTemplateEntriesCount(UUID accountId);
}
