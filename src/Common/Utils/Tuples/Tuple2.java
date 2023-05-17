package Common.Utils.Tuples;

import java.io.Serial;
import java.io.Serializable;

public class Tuple2<T1, T2> implements Serializable {
    @Serial
    private static final long serialVersionUID = -1036387688559959103L;
    private final T1 value1;
    private final T2 value2;

    public Tuple2(final T1 val1, final T2 val2) {
        this.value1 = val1;
        this.value2 = val2;
    }

    public static <T1, T2> Tuple2 create(final T1 val1, final T2 val2) {
        return new Tuple2(val1, val2);
    }

    public T1 getValue1() {
        return value1;
    }

    public T2 getValau2() {
        return value2;
    }

    @Override
    public String toString() {
        return "Pair{" + "1: " + value1 + ", 2: " + value2 + '}';
    }
}
