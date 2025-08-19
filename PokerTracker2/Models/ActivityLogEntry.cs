using System;

namespace PokerTracker2.Models
{
    public class ActivityLogEntry
    {
        public DateTime Timestamp { get; set; }
        public string Message { get; set; } = string.Empty;
        public ActivityType Type { get; set; }

        public ActivityLogEntry(string message, ActivityType type = ActivityType.Info)
        {
            Timestamp = DateTime.Now;
            Message = message;
            Type = type;
        }
    }

    public enum ActivityType
    {
        Info,
        BuyIn,
        CashOut,
        PlayerAdded,
        PlayerRemoved,
        SessionStarted,
        SessionEnded,
        SessionLoaded,
        SessionSaved,
        SessionRenamed,
        SessionHistoryRefreshed,
        SessionHistoryExported,
        SessionDeleted,
        Warning,
        Error
    }
} 