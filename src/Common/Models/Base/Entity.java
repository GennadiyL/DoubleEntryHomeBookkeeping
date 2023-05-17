package Common.Models.Base;

import Common.Models.Interfaces.*;
import java.util.*;

public abstract class Entity implements IEntity {

    private UUID id = UUID.randomUUID();

    public UUID getId() {
        return id;
    }
    public void setId(UUID id) {
        this.id = id;
    }

    public String toString() {
        return String.format("%s %s", this.getClass().getSimpleName(), id.toString());
    }
}
