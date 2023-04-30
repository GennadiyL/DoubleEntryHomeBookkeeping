package Common.Models.Interfaces;

public interface INamedEntity {

    //region Name
    String getName();
    void setName(String value);
    //endregion

    //region Description
    String getDescription();
    void setDescription(String value);
    //endregion
}
