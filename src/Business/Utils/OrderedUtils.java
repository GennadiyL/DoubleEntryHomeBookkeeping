package Business.Utils;

import Common.Models.Interfaces.IOrderedEntity;
import Common.Utils.OrderedEntityComparator;

import java.util.ArrayList;

public class OrderedUtils {
    private static OrderedEntityComparator orderedEntityComparator= new OrderedEntityComparator();

    public static <T extends IOrderedEntity> void reorder(ArrayList<T> entities) {
        entities.sort(orderedEntityComparator);
        for (int i = 0; i < entities.size(); i++) {
            entities.get(i).setOrder(i + 1);
        }
    }

    public static <T extends IOrderedEntity> void setOrder(ArrayList<T> entities, T orderedEntity, int order) {
        for (T entity : entities) {
            if (entity.getOrder() >= order) {
                entity.setOrder(entity.getOrder() + 1);
            }
        }
        orderedEntity.setOrder(order);
        reorder(entities);
    }

    public static <T extends IOrderedEntity> int getNextOrder(ArrayList<T> entities) {
        int max = 0;
        for (T enitiy : entities) {
            int order = enitiy.getOrder();
            if (order > max) {
                max = order;
            }
        }
        return max + 1;
    }
}