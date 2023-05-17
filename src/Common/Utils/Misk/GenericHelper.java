package Common.Utils.Misk;

import java.lang.reflect.*;

public class GenericHelper {

    public static <T> T createGenericType(T outerInstance) {
        return createGenericType(outerInstance, 0);
    }

    public static <T> T createGenericType(T outerInstance, int index) {
        Class<T> innerType = getGenericType(outerInstance, index);
        try {
            return innerType.newInstance();
        } catch (InstantiationException | IllegalAccessException e) {
            return null;
        }
    }

    public static <T> Class<T> getGenericType(T outerInstance) {
        return getGenericType(outerInstance, 0);
    }

    public static <T> Class<T> getGenericType(T outerInstance, int typeIndex) {
        Type outerGenericType = outerInstance.getClass().getGenericSuperclass();
        ParameterizedType outerParameterizedType = (ParameterizedType) outerGenericType;
        Class<T> innerType = (Class) outerParameterizedType.getActualTypeArguments()[typeIndex];
        return innerType;
    }
}
