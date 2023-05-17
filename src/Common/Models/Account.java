package Common.Models;

import Common.Models.Base.*;

public class Account extends ReferenceChildEntity<AccountSubGroup> {

    private Currency currency;
    private Correspondent correspondent;
    private Category category;
    private Project project;

    public Currency getCurrency() {
        return currency;
    }
    public void setCurrency(Currency currency) {
        this.currency = currency;
    }

    public Correspondent getCorrespondent() {
        return correspondent;
    }
    public void setCorrespondent(Correspondent correspondent) {
        this.correspondent = correspondent;
    }

    public Category getCategory() {
        return category;
    }
    public void setCategory(Category category) {
        this.category = category;
    }

    public Project getProject() {
       return project;
    }
    public void setProject(Project project) {
        this.project = project;
    }
}
