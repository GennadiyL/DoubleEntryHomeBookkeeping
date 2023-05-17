package Common.Models.Interfaces;

public interface IChildEntity<T> {
    T getParent();
    void setParent(T value);
}
