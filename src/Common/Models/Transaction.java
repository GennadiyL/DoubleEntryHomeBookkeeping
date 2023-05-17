package Common.Models;

import Common.Models.Base.Entity;
import Common.Models.Enums.TransactionState;
import Common.Models.Interfaces.ITrackedEntity;

import java.time.LocalDateTime;
import java.util.ArrayList;

public class Transaction extends Entity implements ITrackedEntity {

    private String timeStamp;
    private LocalDateTime dateTime;
    private TransactionState state;
    private String comment;
    private final ArrayList<TransactionEntry> entries = new ArrayList<>();

    public String getTimeStamp() {
        return this.timeStamp;
    }
    public void setTimeStamp(String timeStamp) {
        this.timeStamp = timeStamp;
    }

    public LocalDateTime getDateTime() {
        return this.dateTime;
    }
    public void setDateTime(LocalDateTime dateTime) {
        this.dateTime = dateTime;
    }

    public TransactionState getState() {
        return this.state;
    }
    public void setState(TransactionState state) {
        this.state = state;
    }

    public String getComment() {
        return this.comment;
    }
    public void setComment(String comment) {
        this.comment = comment;
    }

    public ArrayList<TransactionEntry> getEntries() {
        return this.entries;
    }
}
