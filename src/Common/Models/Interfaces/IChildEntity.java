package Common.Models.Interfaces;

public interface IChildEntity<T> {
    //region Parent
    T getParent();
    void setParent(T value);
    //endregion
}
