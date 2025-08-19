using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PokerTracker2.Models
{
    public class Player : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _profileId = string.Empty;  // NEW: Link to PlayerProfile
        private double _totalBuyIn;
        private double _totalCashOut;
        private double? _finalStack;  // Set when player leaves session
        private DateTime _firstBuyInTime;
        private DateTime _lastActivityTime;
        private bool _isActive = true;  // NEW: Track if player is still in session

        // NEW: Comprehensive transaction history for audit trail
        private ObservableCollection<PlayerTransaction> _transactionHistory = new ObservableCollection<PlayerTransaction>();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        // NEW: Profile ID for linking to PlayerProfile
        public string ProfileId
        {
            get => _profileId;
            set => SetProperty(ref _profileId, value);
        }

        // NEW: Reference to PlayerProfile (read-only during session)
        public PlayerProfile Profile { get; set; }

        public double TotalBuyIn
        {
            get => _totalBuyIn;
            set 
            { 
                SetProperty(ref _totalBuyIn, value);
                OnPropertyChanged(nameof(CurrentStack));
                OnPropertyChanged(nameof(Profit));
                OnPropertyChanged(nameof(Balance));
                UpdateBuyInGraphData();
            }
        }

        public double TotalCashOut
        {
            get => _totalCashOut;
            set 
            { 
                SetProperty(ref _totalCashOut, value);
                OnPropertyChanged(nameof(CurrentStack));
                OnPropertyChanged(nameof(Profit));
                OnPropertyChanged(nameof(Balance));
            }
        }

        public DateTime FirstBuyInTime
        {
            get => _firstBuyInTime;
            set => SetProperty(ref _firstBuyInTime, value);
        }

        public DateTime LastActivityTime
        {
            get => _lastActivityTime;
            set => SetProperty(ref _lastActivityTime, value);
        }

        public double? FinalStack
        {
            get => _finalStack;
            set 
            { 
                SetProperty(ref _finalStack, value);
                OnPropertyChanged(nameof(CurrentStack));
                OnPropertyChanged(nameof(Profit));
                OnPropertyChanged(nameof(Balance));
            }
        }

        // NEW: Track if player is still active in session
        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        // NEW: Comprehensive transaction history for audit trail
        public ObservableCollection<PlayerTransaction> TransactionHistory
        {
            get => _transactionHistory;
            set => SetProperty(ref _transactionHistory, value);
        }

        // NEW: Get all buy-in transactions
        public IEnumerable<PlayerTransaction> BuyInTransactions => 
            TransactionHistory.Where(t => t.Type == TransactionType.BuyIn);

        // NEW: Get all cash-out transactions
        public IEnumerable<PlayerTransaction> CashOutTransactions => 
            TransactionHistory.Where(t => t.Type == TransactionType.CashOut);

        // NEW: Get total buy-ins from transaction history (more accurate than TotalBuyIn)
        public double CalculatedTotalBuyIn => BuyInTransactions.Sum(t => t.Amount);

        // NEW: Get total cash-outs from transaction history (more accurate than TotalCashOut)
        public double CalculatedTotalCashOut => CashOutTransactions.Sum(t => t.Amount);

        public double Balance => TotalCashOut - TotalBuyIn;  // Keep for backward compatibility
        
        // Current stack: Use final stack if set (player has left), otherwise calculate from buy-in minus cash-out
        public double CurrentStack => _finalStack ?? (TotalBuyIn - TotalCashOut);
        
        // P/L calculation: Total value (current stack + cash-out) minus investment
        public double Profit => (CurrentStack + TotalCashOut) - TotalBuyIn;
        
        // Session activity display properties (computed based on recent transactions)
        private string _recentActivity1 = "• No recent activity";
        private string _recentActivity2 = "";
        
        public string RecentActivity1
        {
            get => _recentActivity1;
            set => SetProperty(ref _recentActivity1, value);
        }
        
        public string RecentActivity2
        {
            get => _recentActivity2;
            set => SetProperty(ref _recentActivity2, value);
        }
        
        // PlayerProfile timeline properties for Database Info display
        public DateTime? ProfileCreatedDate { get; set; }
        public DateTime? ProfileLastPlayedDate { get; set; }
        
        // Buy-in graph data for line graph visualization
        private ObservableCollection<BuyInPoint> _buyInGraphData = new ObservableCollection<BuyInPoint>();
        
        public ObservableCollection<BuyInPoint> BuyInGraphData
        {
            get 
            { 
                System.Diagnostics.Debug.WriteLine($"Player {Name}: BuyInGraphData getter called, current count: {_buyInGraphData?.Count ?? 0}");
                return _buyInGraphData; 
            }
            set 
            { 
                SetProperty(ref _buyInGraphData, value);
                OnPropertyChanged(nameof(BuyInGraphData));
                System.Diagnostics.Debug.WriteLine($"Player {Name}: BuyInGraphData setter called, new count: {value?.Count ?? 0}");
            }
        }
        
        public bool IsEmpty => string.IsNullOrWhiteSpace(Name);
        public string DisplayText => $"{Name}: {TotalBuyIn:C}";

        public Player()
        {
            FirstBuyInTime = DateTime.Now;
            LastActivityTime = DateTime.Now;
            TransactionHistory = new ObservableCollection<PlayerTransaction>();
        }

        public Player(string name) : this()
        {
            Name = name;
        }

        public Player(string name, double initialBuyIn) : this(name)
        {
            // Don't set TotalBuyIn here - let AddBuyIn handle it
            // Ensure initial buy-in is added to transaction history
            if (initialBuyIn > 0)
            {
                System.Diagnostics.Debug.WriteLine($"Player {name}: Constructor adding initial buy-in: {initialBuyIn}");
                AddBuyIn(initialBuyIn, FirstBuyInTime);
            }
        }

        // NEW: Enhanced constructor with ProfileId
        public Player(string name, string profileId, double initialBuyIn = 0) : this(name, initialBuyIn)
        {
            ProfileId = profileId;
        }

        public void Clear()
        {
            Name = string.Empty;
            ProfileId = string.Empty;
            Profile = null;
            TotalBuyIn = 0;
            TotalCashOut = 0;
            FirstBuyInTime = DateTime.MinValue;
            LastActivityTime = DateTime.MinValue;
            IsActive = true;
            TransactionHistory.Clear();
            BuyInGraphData.Clear();
        }

        // NEW: Add buy-in transaction with full audit trail
        public void AddBuyIn(double amount, DateTime timestamp, string note = "")
        {
            if (amount <= 0) return;

            var transaction = new PlayerTransaction
            {
                Type = TransactionType.BuyIn,
                Amount = amount,
                Timestamp = timestamp,
                Note = note,
                TransactionId = Guid.NewGuid().ToString()
            };

            TransactionHistory.Add(transaction);
            
            // Update totals
            TotalBuyIn += amount;
            LastActivityTime = timestamp;
            
            // Update graph data
            UpdateBuyInGraphData();
            
            // Update recent activity display
            UpdateRecentActivity();
            
            System.Diagnostics.Debug.WriteLine($"Player {Name}: Added buy-in transaction - Amount: {amount}, Total buy-ins: {TotalBuyIn}");
        }

        // NEW: Add cash-out transaction with full audit trail
        public void AddCashOut(double amount, DateTime timestamp, string note = "")
        {
            if (amount <= 0) return;

            var transaction = new PlayerTransaction
            {
                Type = TransactionType.CashOut,
                Amount = amount,
                Timestamp = timestamp,
                Note = note,
                TransactionId = Guid.NewGuid().ToString()
            };

            TransactionHistory.Add(transaction);
            
            // Update totals
            TotalCashOut += amount;
            LastActivityTime = timestamp;
            
            // Update recent activity display
            UpdateRecentActivity();
            
            System.Diagnostics.Debug.WriteLine($"Player {Name}: Added cash-out transaction - Amount: {amount}, Total cash-outs: {TotalCashOut}");
        }

        // NEW: Remove transaction (for corrections)
        public bool RemoveTransaction(string transactionId)
        {
            var transaction = TransactionHistory.FirstOrDefault(t => t.TransactionId == transactionId);
            if (transaction == null) return false;

            TransactionHistory.Remove(transaction);
            
            // Recalculate totals from remaining transactions
            RecalculateTotalsFromHistory();
            
            // Update graph data
            UpdateBuyInGraphData();
            
            // Update recent activity display
            UpdateRecentActivity();
            
            return true;
        }

        // NEW: Recalculate totals from transaction history (for data integrity)
        private void RecalculateTotalsFromHistory()
        {
            TotalBuyIn = CalculatedTotalBuyIn;
            TotalCashOut = CalculatedTotalCashOut;
        }

        // NEW: Update recent activity display based on transaction history
        private void UpdateRecentActivity()
        {
            var recentTransactions = TransactionHistory
                .OrderByDescending(t => t.Timestamp)
                .Take(2)
                .ToList();

            if (recentTransactions.Count == 0)
            {
                RecentActivity1 = "• No recent activity";
                RecentActivity2 = "";
                return;
            }

            if (recentTransactions.Count >= 1)
            {
                var t1 = recentTransactions[0];
                RecentActivity1 = $"• {t1.Type} {t1.Amount:C} at {t1.Timestamp:HH:mm}";
            }

            if (recentTransactions.Count >= 2)
            {
                var t2 = recentTransactions[1];
                RecentActivity2 = $"• {t2.Type} {t2.Amount:C} at {t2.Timestamp:HH:mm}";
            }
        }

        // NEW: Get transaction summary for session completion
        public PlayerSessionResult GetSessionResult()
        {
            return new PlayerSessionResult
            {
                ProfileId = ProfileId,
                PlayerName = Name,
                SessionStartTime = FirstBuyInTime,
                SessionEndTime = DateTime.Now,
                TotalBuyIns = CalculatedTotalBuyIn,
                TotalCashOuts = CalculatedTotalCashOut,
                FinalStack = FinalStack ?? CurrentStack,
                TransactionCount = TransactionHistory.Count,
                TransactionHistory = TransactionHistory.ToList()
            };
        }

        // NEW: Validate transaction history integrity
        public bool ValidateTransactionIntegrity()
        {
            var calculatedBuyIn = CalculatedTotalBuyIn;
            var calculatedCashOut = CalculatedTotalCashOut;
            
            // Check if stored totals match calculated totals
            var totalsMatch = Math.Abs(TotalBuyIn - calculatedBuyIn) < 0.01 && 
                             Math.Abs(TotalCashOut - calculatedCashOut) < 0.01;
            
            if (!totalsMatch)
            {
                System.Diagnostics.Debug.WriteLine($"Player {Name}: Transaction integrity check failed - Stored: BuyIn={TotalBuyIn}, CashOut={TotalCashOut}; Calculated: BuyIn={calculatedBuyIn}, CashOut={calculatedCashOut}");
            }
            
            return totalsMatch;
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
        
        // Method to add a buy-in point to the graph data
        public void AddBuyInPoint(double amount, DateTime timestamp)
        {
            System.Diagnostics.Debug.WriteLine($"Player {Name}: AddBuyInPoint called with amount: {amount}");
            
            // Calculate cumulative amount by adding to the previous total
            var cumulativeAmount = BuyInGraphData.Count > 0 ? 
                BuyInGraphData[BuyInGraphData.Count - 1].Amount + amount : amount;
            
            System.Diagnostics.Debug.WriteLine($"Player {Name}: Previous amount: {(BuyInGraphData.Count > 0 ? BuyInGraphData[BuyInGraphData.Count - 1].Amount : 0)}, New amount: {amount}, Cumulative: {cumulativeAmount}");
            
            BuyInGraphData.Add(new BuyInPoint 
            { 
                Amount = cumulativeAmount, 
                Timestamp = timestamp 
            });
            
            // Add debugging
            System.Diagnostics.Debug.WriteLine($"Player {Name}: Added buy-in point - Amount: {cumulativeAmount}, Total points: {BuyInGraphData.Count}");
            System.Diagnostics.Debug.WriteLine($"Player {Name}: BuyInGraphData.Count = {BuyInGraphData.Count}");
        }
        
        // Method to update the graph data when buy-in changes
        public void UpdateBuyInGraphData()
        {
            // Clear existing graph data
            BuyInGraphData.Clear();
            
            // Build graph data from transaction history
            var cumulativeAmount = 0.0;
            var buyInTransactions = BuyInTransactions.OrderBy(t => t.Timestamp).ToList();
            
            foreach (var transaction in buyInTransactions)
            {
                cumulativeAmount += transaction.Amount;
                BuyInGraphData.Add(new BuyInPoint 
                { 
                    Amount = cumulativeAmount, 
                    Timestamp = transaction.Timestamp 
                });
            }
        }
    }
    
    // NEW: Enhanced PlayerTransaction class for comprehensive audit trail
    public class PlayerTransaction
    {
        public string TransactionId { get; set; } = string.Empty;
        public TransactionType Type { get; set; }  // Use existing TransactionType enum
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
        public string Note { get; set; } = string.Empty;
        public string SessionId { get; set; } = string.Empty;  // Link to session for cross-reference
    }

    // NEW: PlayerSessionResult for session completion (renamed to avoid conflicts)
    public class PlayerSessionResult
    {
        public string ProfileId { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public DateTime SessionStartTime { get; set; }
        public DateTime SessionEndTime { get; set; }
        public double TotalBuyIns { get; set; }
        public double TotalCashOuts { get; set; }
        public double FinalStack { get; set; }
        public int TransactionCount { get; set; }
        public List<PlayerTransaction> TransactionHistory { get; set; } = new List<PlayerTransaction>();
        
        // Calculated properties
        public double SessionProfit => (FinalStack + TotalCashOuts) - TotalBuyIns;
        public TimeSpan SessionDuration => SessionEndTime - SessionStartTime;
    }
    
    // BuyInPoint class for the line graph
    public class BuyInPoint
    {
        public double Amount { get; set; }
        public DateTime Timestamp { get; set; }
    }
} 