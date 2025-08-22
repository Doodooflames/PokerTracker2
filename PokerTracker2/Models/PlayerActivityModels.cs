using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace PokerTracker2.Models
{
    // Model for session breakdown display
    public class SessionBreakdownItem
    {
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public double BuyIns { get; set; }
        public double CashOuts { get; set; }
        public double FinalStack { get; set; }
        public double Profit { get; set; }
        public string SessionId { get; set; } = string.Empty;
        public List<TransactionHistoryItem> Transactions { get; set; } = new List<TransactionHistoryItem>();
        
        // Calculated properties
        public string SessionDateDisplay => SessionDate.ToString("MMM dd, yyyy");
        
        // Property for profit color binding
        public Brush ProfitColor
        {
            get
            {
                try
                {
                    return Profit >= 0 ? new SolidColorBrush(Colors.Green) : new SolidColorBrush(Colors.Red);
                }
                catch
                {
                    return new SolidColorBrush(Colors.Gray);
                }
            }
        }

        // Display-friendly combined cash-out summary: Final stack + partial cash-outs = total
        public string CashOutSummaryDisplay
        {
            get
            {
                try
                {
                    string summary = $"💵 Cash-outs: Final stack ({FinalStack.ToString("C")})";
                    if (CashOuts > 0)
                    {
                        summary += $" + partials ({CashOuts.ToString("C")})";
                    }
                    var total = (FinalStack + CashOuts).ToString("C");
                    summary += $" = {total}";
                    return summary;
                }
                catch
                {
                    return "💵 Cash-outs: N/A";
                }
            }
        }
    }
    
    // Model for transaction history display
    public class TransactionHistoryItem
    {
        public string TypeIcon { get; set; } = string.Empty;
        public Brush TypeColor { get; set; } = new SolidColorBrush(Colors.White);
        public double Amount { get; set; }
        public string Notes { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public TransactionType Type { get; set; }
        
        // Static factory methods for different transaction types
        public static TransactionHistoryItem CreateBuyIn(double amount, string notes, string sessionName, DateTime timestamp)
        {
            return new TransactionHistoryItem
            {
                TypeIcon = "💰",
                TypeColor = new SolidColorBrush(Colors.LightBlue),
                Amount = amount,
                Notes = notes,
                SessionName = sessionName,
                Timestamp = timestamp,
                Type = TransactionType.BuyIn
            };
        }
        
        public static TransactionHistoryItem CreateCashOut(double amount, string notes, string sessionName, DateTime timestamp)
        {
            return new TransactionHistoryItem
            {
                TypeIcon = "💵",
                TypeColor = new SolidColorBrush(Colors.LightGreen),
                Amount = amount,
                Notes = notes,
                SessionName = sessionName,
                Timestamp = timestamp,
                Type = TransactionType.CashOut
            };
        }
        
        public static TransactionHistoryItem CreateTransfer(double amount, string notes, string sessionName, DateTime timestamp)
        {
            return new TransactionHistoryItem
            {
                TypeIcon = "🔄",
                TypeColor = new SolidColorBrush(Colors.LightYellow),
                Amount = amount,
                Notes = notes,
                SessionName = sessionName,
                Timestamp = timestamp,
                Type = TransactionType.Transfer
            };
        }
    }
    
    // Model for profit trend data (extends BuyInPoint for the line graph)
    public class ProfitPoint
    {
        public double Profit { get; set; }
        public DateTime Timestamp { get; set; }
        public string SessionId { get; set; } = string.Empty;
    }
}
