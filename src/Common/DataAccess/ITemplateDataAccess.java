package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.*;

import java.util.ArrayList;
import java.util.UUID;

public interface ITemplateDataAccess extends IReferenceChildEntityDataAccess<Template>{
    ArrayList<TemplateEntry> getEntriesByAccount(Account account);
    int getTemplateEntriesCount(UUID accountId);
}
