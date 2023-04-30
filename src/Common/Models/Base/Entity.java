package Common.Models.Base;

import Common.Models.Interfaces.IEntity;
import java.util.UUID;
import static java.util.UUID.randomUUID;

public abstract class Entity implements IEntity {

    //region Id
    private UUID id = randomUUID();
    @Override
    public UUID getId() {
        return id;
    }
    @Override
    public void setId(UUID id) {
        this.id = id;
    }
    //endregion

    @Override
    public String toString() {
        return String.format("%s %s", this.getClass().getSimpleName(), id.toString());
    }
}
