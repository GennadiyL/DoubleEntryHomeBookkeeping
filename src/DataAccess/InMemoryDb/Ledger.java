package DataAccess.InMemoryDb;

import Common.Models.*;
import Common.Models.Currency;
import Common.Models.Interfaces.IEntity;

import java.util.*;

public class Ledger {

    //region Fields
    private ArrayList<AccountGroup> accountGroups = new ArrayList<AccountGroup>();
    private ArrayList<AccountSubGroup> accountSubGroups = new ArrayList<AccountSubGroup>();
    private ArrayList<Account> accounts = new ArrayList<Account>();
    private ArrayList<CategoryGroup> categoryGroups = new ArrayList<CategoryGroup>();
    private ArrayList<Category> categories = new ArrayList<Category>();
    private ArrayList<CorrespondentGroup> correspondentGroups = new ArrayList<CorrespondentGroup>();
    private ArrayList<Correspondent> correspondents = new ArrayList<Correspondent>();
    private ArrayList<Currency> currencies = new ArrayList<Currency>();
    private ArrayList<CurrencyRate> currencyRates = new ArrayList<CurrencyRate>();
    private ArrayList<ProjectGroup> projectGroups = new ArrayList<ProjectGroup>();
    private ArrayList<Project> projects = new ArrayList<Project>();
    private ArrayList<TemplateGroup> templateGroups = new ArrayList<TemplateGroup>();
    private ArrayList<Template> templates = new ArrayList<Template>();
    private ArrayList<TemplateEntry> templateEntries = new ArrayList<TemplateEntry>();
    private ArrayList<Transaction> transactions = new ArrayList<Transaction>();
    private ArrayList<TransactionEntry> transactionEntries = new ArrayList<TransactionEntry>();

    private ArrayList<UUID> addedIds = new ArrayList<UUID>();
    private ArrayList<UUID> updatedIds = new ArrayList<UUID>();
    private ArrayList<UUID> deletedIds = new ArrayList<UUID>();
    //endregion

    //region Getters
    public ArrayList<AccountGroup> getAccountGroups() {
        return accountGroups;
    }

    public ArrayList<AccountSubGroup> getAccountSubGroups() {
        return accountSubGroups;
    }

    public ArrayList<Account> getAccounts() {
        return accounts;
    }

    public ArrayList<CategoryGroup> getCategoryGroups() {
        return categoryGroups;
    }

    public ArrayList<Category> getCategories() {
        return categories;
    }

    public ArrayList<CorrespondentGroup> getCorrespondentGroups() {
        return correspondentGroups;
    }

    public ArrayList<Correspondent> getCorrespondents() {
        return correspondents;
    }

    public ArrayList<Currency> getCurrencies() {
        return currencies;
    }

    public ArrayList<CurrencyRate> getCurrencyRates() {
        return currencyRates;
    }

    public ArrayList<ProjectGroup> getProjectGroups() {
        return projectGroups;
    }

    public ArrayList<Project> getProjects() {
        return projects;
    }

    public ArrayList<TemplateGroup> getTemplateGroups() {
        return templateGroups;
    }

    public ArrayList<Template> getTemplates() {
        return templates;
    }

    public ArrayList<TemplateEntry> getTemplateEntries() {
        return templateEntries;
    }

    public ArrayList<Transaction> getTransactions() {
        return transactions;
    }

    public ArrayList<TransactionEntry> getTransactionEntries() {
        return transactionEntries;
    }

    public ArrayList<UUID> getAddedIds() {
        return addedIds;
    }

    public ArrayList<UUID> getUpdatedIds() {
        return updatedIds;
    }

    public ArrayList<UUID> getDeletedIds() {
        return deletedIds;
    }

    //endregion
}
