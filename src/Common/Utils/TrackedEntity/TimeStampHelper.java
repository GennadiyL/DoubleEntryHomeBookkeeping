package Common.Utils.TrackedEntity;

import org.jetbrains.annotations.NotNull;

import java.time.*;
import java.time.format.DateTimeFormatter;

public class TimeStampHelper {

    public static final DateTimeFormatter DATE_TIME_FORMATTER = DateTimeFormatter.ofPattern("yyyyMMddHHmmssSSSSSS");
    public static final DateTimeFormatter DATE_FORMATTER = DateTimeFormatter.ofPattern("yyyyMMdd");
    public static final ZoneId UTC_ZONE_ID = ZoneId.of("UTC");
    public static final ZoneId CURRENT_ZONE_ID = ZoneId.systemDefault();

    public static  String getDateTimeStamp( LocalDateTime dateTime) {
        return dateTime.format(DATE_TIME_FORMATTER);
    }

    public static  String getDateTimeStamp() {
        LocalDateTime dateTime = LocalDateTime.now(UTC_ZONE_ID);
        return getDateTimeStamp(dateTime);
    }

    public static  String getDateStamp( LocalDate date) {
        return date.format(DATE_FORMATTER);
    }

    public static  String getDateStamp() {
        LocalDate time = LocalDate.now(UTC_ZONE_ID);
        return time.format(DATE_TIME_FORMATTER);
    }

    public static LocalDateTime getDateTimeFromStamp(String stamp) {
        return LocalDateTime.parse(stamp, DATE_TIME_FORMATTER);
    }

    public static LocalDate getDateFromStamp(String stamp) {
        return LocalDate.parse(stamp, DATE_FORMATTER);
    }
}
