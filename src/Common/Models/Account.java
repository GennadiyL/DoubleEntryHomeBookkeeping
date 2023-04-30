package Common.Models;

import Common.Models.Base.ReferenceChildEntity;

public class Account extends ReferenceChildEntity<AccountSubGroup> {

    //region Сurrency
    private Currency currency;
    public Currency getCurrency() {
        return currency;
    }
    public void setCurrency(Currency currency) {
        this.currency = currency;
    }
    //endregion

    //region Сorrespondent
    private Correspondent correspondent;
    public Correspondent getCorrespondent() {
        return correspondent;
    }
    public void setCorrespondent(Correspondent correspondent) {
        this.correspondent = correspondent;
    }
    //endregion

    //region Сategory
    private Category category;
    public Category getCategory() {
        return category;
    }
    public void setCategory(Category category) {
        this.category = category;
    }
    //endregion

    //region Project
    private Project project;
    public Project getProject() {
       return project;
    }
    public void setProject(Project project) {
        this.project = project;
    }
    //endregion


}
