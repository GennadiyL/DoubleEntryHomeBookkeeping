package Common.Utils;

import java.io.Serial;
import java.io.Serializable;

public class Tuple2<T,U> implements Serializable {
    @Serial
    private static final long serialVersionUID = -1036387688559959103L;
    private final T value1;
    private final U value2;

    public Tuple2(final T val1, final U val2) {
        this.value1 = val1;
        this.value2 = val2;
    }

    public static <T, U> Tuple2 create(final T val1, final U val2) {
        return new Tuple2(val1, val2);
    }

    public T getValue1() {
        return value1;
    }

    public U getValau2() {
        return value2;
    }

    @Override
    public String toString() {
        return "Pair{" + "1: " + value1 + ", 2: " + value2 + '}';
    }
}
