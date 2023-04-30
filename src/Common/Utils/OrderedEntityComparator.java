package Common.Utils;

import Common.Models.Interfaces.IOrderedEntity;

import java.util.Comparator;

public class OrderedEntityComparator implements Comparator<IOrderedEntity> {
    @Override
    public int compare(IOrderedEntity o1, IOrderedEntity o2) {
        return o1.getOrder() - o2.getOrder();
    }
}
