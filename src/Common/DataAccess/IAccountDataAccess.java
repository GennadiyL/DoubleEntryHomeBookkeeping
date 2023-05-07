package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.*;

import java.util.ArrayList;

public interface IAccountDataAccess extends IReferenceChildEntityDataAccess<Account> {
    //todo probably change to getCurrency
    void loadCurrency(Account account);
    ArrayList<Account> getAccountsByCorrespondent(Correspondent correspondent);
    ArrayList<Account> getAccountsByCategory(Category category);
    ArrayList<Account> getAccountsByProject(Project project);
}

