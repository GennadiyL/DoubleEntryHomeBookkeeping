package Common.Models;

import Common.Models.Base.Entity;
import Common.Models.Enums.TransactionState;
import Common.Models.Interfaces.ITrackedEntity;

import java.time.LocalDateTime;
import java.util.ArrayList;

public class Transaction extends Entity implements ITrackedEntity {

    //region TimeStamp
    String timeStamp;
    @Override
    public String getTimeStamp() {
        return this.timeStamp;
    }
    @Override
    public void setTimeStamp(String timeStamp) {
        this.timeStamp = timeStamp;
    }
    //endregion

    //region DateTime
    LocalDateTime dateTime;
    public LocalDateTime getDateTime() {
        return this.dateTime;
    }
    public void setDateTime(LocalDateTime dateTime) {
        this.dateTime = dateTime;
    }
    //endregion

    //region State
    TransactionState state;
    public TransactionState getState() {
        return this.state;
    }
    public void setState(TransactionState state) {
        this.state = state;
    }
    //endregion

    //region Comment
    String comment;
    public String getComment() {
        return this.comment;
    }
    public void setComment(String comment) {
        this.comment = comment;
    }
    //endregion

    //region Entries
    ArrayList<TransactionEntry> entries = new ArrayList<>();
    public ArrayList<TransactionEntry> getEntries() {
        return this.entries;
    }
    //endregion
}
