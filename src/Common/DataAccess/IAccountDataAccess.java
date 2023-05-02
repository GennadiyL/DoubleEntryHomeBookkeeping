package Common.DataAccess;

import Common.DataAccess.Base.*;
import Common.Models.*;

import java.util.ArrayList;

public interface IAccountDataAccess extends IReferenceChildEntityDataAccess<Account> {
    void LoadCurrency(Account account);
    ArrayList<Account> GetAccountsByCorrespondent(Correspondent correspondent);
    ArrayList<Account> GetAccountsByCategory(Category category);
    ArrayList<Account> GetAccountsByProject(Project project);
}

