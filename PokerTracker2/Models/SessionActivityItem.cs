using System;
using System.Windows;
using System.Globalization;

namespace PokerTracker2.Models
{
    /// <summary>
    /// Data model for displaying session activity items in the Session Activity panel
    /// </summary>
    public class SessionActivityItem
    {
        public string PlayerName { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Note { get; set; } = string.Empty;
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public TransactionType Type { get; set; }
        
        // Display properties
        public string TypeIcon => Type switch
        {
            TransactionType.BuyIn => "💰",
            TransactionType.CashOut => "💸",
            _ => "❓"
        };
        
        public string TypeColor => Type switch
        {
            TransactionType.BuyIn => "#4CAF50", // Green for buy-ins
            TransactionType.CashOut => "#FF9800", // Orange for cash-outs
            _ => "#9E9E9E" // Gray for unknown
        };
        
        public string AmountDisplay => Amount.ToString("C", CultureInfo.CurrentCulture);
        
        public string Time => Timestamp.ToString("HH:mm");
        
        public Visibility NoteVisibility => string.IsNullOrWhiteSpace(Note) ? Visibility.Collapsed : Visibility.Visible;
        
        public SessionActivityItem()
        {
        }
        
        public SessionActivityItem(PlayerTransaction transaction, string playerName)
        {
            PlayerName = playerName;
            Type = transaction.Type;
            Amount = transaction.Amount;
            Timestamp = transaction.Timestamp;
            Note = transaction.Note;
            
            Description = Type switch
            {
                TransactionType.BuyIn => "Buy-in",
                TransactionType.CashOut => "Cash-out",
                _ => "Transaction"
            };
        }
        
        public SessionActivityItem(Transaction transaction)
        {
            PlayerName = transaction.PlayerName;
            Type = transaction.Type;
            Amount = transaction.Amount;
            Timestamp = transaction.Timestamp;
            Note = transaction.Notes ?? string.Empty;
            
            Description = Type switch
            {
                TransactionType.BuyIn => "Buy-in",
                TransactionType.CashOut => "Cash-out",
                _ => "Transaction"
            };
        }
    }
}
