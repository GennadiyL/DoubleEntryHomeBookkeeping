package DataAccess.InMemoryDb;

import Common.Models.*;
import Common.Models.Currency;
import Common.Models.Interfaces.IEntity;

import java.util.*;

public class Ledger {

    //region Fields
    private final ArrayList<AccountGroup> accountGroups = new ArrayList<AccountGroup>();
    private final ArrayList<AccountSubGroup> accountSubGroups = new ArrayList<AccountSubGroup>();
    private final ArrayList<Account> accounts = new ArrayList<Account>();
    private final ArrayList<CategoryGroup> categoryGroups = new ArrayList<CategoryGroup>();
    private final ArrayList<Category> categories = new ArrayList<Category>();
    private final ArrayList<CorrespondentGroup> correspondentGroups = new ArrayList<CorrespondentGroup>();
    private final ArrayList<Correspondent> correspondents = new ArrayList<Correspondent>();
    private final ArrayList<Currency> currencies = new ArrayList<Currency>();
    private final ArrayList<CurrencyRate> currencyRates = new ArrayList<CurrencyRate>();
    private final ArrayList<ProjectGroup> projectGroups = new ArrayList<ProjectGroup>();
    private final ArrayList<Project> projects = new ArrayList<Project>();
    private final ArrayList<TemplateGroup> templateGroups = new ArrayList<TemplateGroup>();
    private final ArrayList<Template> templates = new ArrayList<Template>();
    private final ArrayList<TemplateEntry> templateEntries = new ArrayList<TemplateEntry>();
    private final ArrayList<Transaction> transactions = new ArrayList<Transaction>();
    private final ArrayList<TransactionEntry> transactionEntries = new ArrayList<TransactionEntry>();

    private final ArrayList<ArrayList<? extends IEntity>> allLists = new ArrayList<>();

    private final ArrayList<UUID> addedIds = new ArrayList<UUID>();
    private final ArrayList<UUID> updatedIds = new ArrayList<UUID>();
    private final ArrayList<UUID> deletedIds = new ArrayList<UUID>();
    //endregion


    public Ledger() {
        allLists.add(accountGroups);
        allLists.add(accountSubGroups);
        allLists.add(accounts);
        allLists.add(categoryGroups);
        allLists.add(categories);
        allLists.add(correspondentGroups);
        allLists.add(correspondents);
        allLists.add(currencies);
        allLists.add(currencyRates);
        allLists.add(projectGroups);
        allLists.add(projects);
        allLists.add(templateGroups);
        allLists.add(templates);
        allLists.add(templateEntries);
        allLists.add(transactions);
        allLists.add(transactionEntries);
    }

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

    public ArrayList<ArrayList<? extends IEntity>> getAllLists() {
        return allLists;
    }

    //endregion
}
