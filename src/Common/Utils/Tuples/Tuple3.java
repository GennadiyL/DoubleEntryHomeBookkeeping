package Common.Utils.Tuples;

import java.io.Serial;
import java.io.Serializable;

public class Tuple3<T1, T2, T3> implements Serializable {
    @Serial
    private static final long serialVersionUID = -6680803833894375373L;
    private final T1 value1;
    private final T2 value2;
    private final T3 value3;

    public Tuple3(final T1 val1, final T2 val2, final T3 val3) {
        this.value1 = val1;
        this.value2 = val2;
        this.value3 = val3;
    }

    public static <T1, T2, T3> Tuple3 create(final T1 val1, final T2 val2, final T3 val3) {
        return new Tuple3(val1, val2, val3);
    }

    public T1 getValue1() {
        return value1;
    }

    public T2 getValau2() {
        return value2;
    }

    public T3 getValau3() {
        return value3;
    }

    @Override
    public String toString() {
        return "Tuple3 {" + "1: " + value1 + ", 2: " + value2 + ", 3: " + value3 + '}';
    }
}
