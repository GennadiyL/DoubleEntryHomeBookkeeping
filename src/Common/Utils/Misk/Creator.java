package Common.Utils.Misk;

import java.lang.reflect.*;

public class Creator {

    public static <T> T createGenericType(T service) {
        return createGenericType(service, 0);
    }

    public static <T> T createGenericType(T service, int typeIndex) {
        Type serviceGenericType = service.getClass().getGenericSuperclass();;
        ParameterizedType serviceParameterizedType = (ParameterizedType) serviceGenericType;
        Class<T> entityType = (Class) serviceParameterizedType.getActualTypeArguments()[typeIndex];
        try {
            return entityType.newInstance();
        } catch (InstantiationException | IllegalAccessException e) {
            return null;
        }
    }
}
