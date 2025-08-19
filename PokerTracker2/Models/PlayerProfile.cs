using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Linq; // Added for .OrderByDescending() and .Take()

namespace PokerTracker2.Models
{
    public class PlayerProfile : INotifyPropertyChanged
    {
        private string _name = string.Empty;
        private string _nickname = string.Empty;
        private string _email = string.Empty;
        private string _phone = string.Empty;
        private string _notes = string.Empty;
        private string _passwordHash = string.Empty;
        private string _salt = string.Empty;
        private bool _hasPassword = false;
        private DateTime _createdDate;
        private DateTime _lastPlayedDate;
        private int _totalSessionsPlayed;
        private double _totalLifetimeBuyIn;
        private double _totalLifetimeCashOut;
        private bool _isActive = true;
        private bool _isAdmin = false;
        private List<string> _sessionIds = new List<string>();
        private List<PlayerSessionSummary> _recentSessions = new List<PlayerSessionSummary>();

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Nickname
        {
            get => _nickname;
            set => SetProperty(ref _nickname, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string Phone
        {
            get => _phone;
            set => SetProperty(ref _phone, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public string PasswordHash
        {
            get => _passwordHash;
            set => SetProperty(ref _passwordHash, value);
        }

        public string Salt
        {
            get => _salt;
            set => SetProperty(ref _salt, value);
        }

        public bool HasPassword
        {
            get => _hasPassword;
            set => SetProperty(ref _hasPassword, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime LastPlayedDate
        {
            get => _lastPlayedDate;
            set => SetProperty(ref _lastPlayedDate, value);
        }

        public int TotalSessionsPlayed
        {
            get => _sessionIds.Count;
            set => SetProperty(ref _totalSessionsPlayed, value);
        }

        public double TotalLifetimeBuyIn
        {
            get => _totalLifetimeBuyIn;
            set => SetProperty(ref _totalLifetimeBuyIn, value);
        }

        public double TotalLifetimeCashOut
        {
            get => _totalLifetimeCashOut;
            set => SetProperty(ref _totalLifetimeCashOut, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
        }

        public bool IsAdmin
        {
            get => _isAdmin;
            set => SetProperty(ref _isAdmin, value);
        }

        public List<string> SessionIds
        {
            get => _sessionIds;
            set => SetProperty(ref _sessionIds, value);
        }

        public List<PlayerSessionSummary> RecentSessions
        {
            get => _recentSessions;
            set => SetProperty(ref _recentSessions, value);
        }

        // Computed properties
        public double LifetimeBalance => TotalLifetimeCashOut - TotalLifetimeBuyIn;
        public double LifetimeProfit => LifetimeBalance;
        public string DisplayName => string.IsNullOrWhiteSpace(Nickname) ? Name : $"{Name} ({Nickname})";
        public string SearchText => $"{Name} {Nickname} {Email}".ToLower();
        public bool IsEmpty => string.IsNullOrWhiteSpace(Name);

        public PlayerProfile()
        {
            CreatedDate = DateTime.UtcNow;
            LastPlayedDate = DateTime.UtcNow;
        }

        public PlayerProfile(string name) : this()
        {
            Name = name;
        }

        public PlayerProfile(string name, string nickname = "") : this(name)
        {
            Nickname = nickname;
        }

        // Password management methods
        public void SetPassword(string password)
        {
            if (string.IsNullOrWhiteSpace(password))
            {
                HasPassword = false;
                PasswordHash = string.Empty;
                Salt = string.Empty;
                return;
            }

            // Generate a random salt
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            Salt = Convert.ToBase64String(saltBytes);

            // Hash the password with the salt
            using (var sha256 = SHA256.Create())
            {
                var passwordWithSalt = password + Salt;
                var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
                PasswordHash = Convert.ToBase64String(hashBytes);
            }

            HasPassword = true;
        }

        public bool VerifyPassword(string password)
        {
            if (!HasPassword || string.IsNullOrWhiteSpace(password))
                return false;

            try
            {
                using (var sha256 = SHA256.Create())
                {
                    var passwordWithSalt = password + Salt;
                    var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(passwordWithSalt));
                    var computedHash = Convert.ToBase64String(hashBytes);
                    return PasswordHash == computedHash;
                }
            }
            catch
            {
                return false;
            }
        }

        public void ClearPassword()
        {
            HasPassword = false;
            PasswordHash = string.Empty;
            Salt = string.Empty;
        }

        public void UpdateLastPlayed()
        {
            LastPlayedDate = DateTime.UtcNow;
        }

        /// <summary>
        /// Add a session reference to this player's profile
        /// </summary>
        public void AddSessionReference(string sessionId, string sessionName, DateTime sessionDate, double buyIn, double cashOut, TimeSpan duration, int playerCount)
        {
            if (!_sessionIds.Contains(sessionId))
            {
                _sessionIds.Add(sessionId);
            }

            // DON'T add to recent sessions during active sessions - wait until session is completed
            // var sessionSummary = new PlayerSessionSummary(sessionId, sessionName, sessionDate, buyIn, cashOut, duration, playerCount);
            // _recentSessions.Add(sessionSummary);
            // if (_recentSessions.Count > 10)
            // {
            //     _recentSessions.RemoveAt(0);
            // }

            // DON'T update totals here - wait until session is completed
            // AddSessionStats(buyIn, cashOut);
        }

        /// <summary>
        /// Update session reference with new buy-in/cash-out data
        /// </summary>
        public void UpdateSessionReference(string sessionId, double additionalBuyIn, double additionalCashOut)
        {
            // Find existing session summary and update it
            var existingSession = _recentSessions.FirstOrDefault(s => s.SessionId == sessionId);
            if (existingSession != null)
            {
                existingSession.BuyIn += additionalBuyIn;
                existingSession.CashOut += additionalCashOut;
                
                // Update duration and player count to current values
                existingSession.Duration = DateTime.UtcNow - existingSession.SessionDate;
                // Note: Player count is not updated here as it's session-level, not player-level
            }

            // DON'T update lifetime totals here - wait until session is completed
            // TotalLifetimeBuyIn += additionalBuyIn;
            // TotalLifetimeCashOut += additionalCashOut;
            // UpdateLastPlayed();
        }

        /// <summary>
        /// Finalize session and update lifetime totals with final session data
        /// </summary>
        public void FinalizeSession(string sessionId, double totalSessionBuyIns, double totalSessionCashOuts, string sessionName, DateTime sessionDate, TimeSpan duration, int playerCount)
        {
            // Add the completed session to recent sessions
            var sessionSummary = new PlayerSessionSummary(sessionId, sessionName, sessionDate, totalSessionBuyIns, totalSessionCashOuts, duration, playerCount);
            _recentSessions.Add(sessionSummary);
            if (_recentSessions.Count > 10)
            {
                _recentSessions.RemoveAt(0);
            }
            
            // Add the completed session to lifetime totals
            TotalLifetimeBuyIn += totalSessionBuyIns;
            TotalLifetimeCashOut += totalSessionCashOuts;
            
            // Update last played date
            UpdateLastPlayed();
        }

        /// <summary>
        /// Get the total number of sessions this player has participated in
        /// </summary>
        public int GetTotalSessionsCount()
        {
            return _sessionIds.Count;
        }

        /// <summary>
        /// Check if player has participated in a specific session
        /// </summary>
        public bool HasParticipatedInSession(string sessionId)
        {
            return _sessionIds.Contains(sessionId);
        }

        /// <summary>
        /// Clear all session data and reset totals (for fixing corrupted data)
        /// </summary>
        public void ClearSessionData()
        {
            _sessionIds.Clear();
            _recentSessions.Clear();
            TotalSessionsPlayed = 0;
            TotalLifetimeBuyIn = 0;
            TotalLifetimeCashOut = 0;
        }

        /// <summary>
        /// Recalculate totals from session data (for fixing corrupted data)
        /// </summary>
        public void RecalculateTotalsFromSessions()
        {
            TotalSessionsPlayed = _sessionIds.Count;
            TotalLifetimeBuyIn = _recentSessions.Sum(s => s.BuyIn);
            TotalLifetimeCashOut = _recentSessions.Sum(s => s.CashOut);
        }

        /// <summary>
        /// Get profit/loss trend from recent sessions
        /// </summary>
        public List<double> GetRecentProfitTrend(int count = 10)
        {
            return _recentSessions
                .OrderByDescending(s => s.SessionDate)
                .Take(count)
                .Select(s => s.Profit)
                .Reverse() // Oldest to newest for trend analysis
                .ToList();
        }

        /// <summary>
        /// Get average profit per session
        /// </summary>
        public double GetAverageProfitPerSession()
        {
            if (_recentSessions.Count == 0) return 0;
            return _recentSessions.Average(s => s.Profit);
        }

        /// <summary>
        /// Get best and worst session performance
        /// </summary>
        public (double BestProfit, double WorstProfit) GetBestWorstPerformance()
        {
            if (_recentSessions.Count == 0) return (0, 0);
            
            var profits = _recentSessions.Select(s => s.Profit).ToList();
            return (profits.Max(), profits.Min());
        }

        public void Clear()
        {
            Name = string.Empty;
            Nickname = string.Empty;
            Email = string.Empty;
            Phone = string.Empty;
            Notes = string.Empty;
            ClearPassword();
            CreatedDate = DateTime.MinValue;
            LastPlayedDate = DateTime.MinValue;
            TotalSessionsPlayed = 0;
            TotalLifetimeBuyIn = 0;
            TotalLifetimeCashOut = 0;
            IsActive = true;
            _sessionIds.Clear();
            _recentSessions.Clear();
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

        public override string ToString()
        {
            return DisplayName;
        }

        public override bool Equals(object? obj)
        {
            if (obj is PlayerProfile other)
            {
                return string.Equals(Name, other.Name, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Name?.ToLower().GetHashCode() ?? 0;
        }
    }

    /// <summary>
    /// Summary of a player's performance in a specific session for analytics
    /// </summary>
    public class PlayerSessionSummary
    {
        public string SessionId { get; set; } = string.Empty;
        public string SessionName { get; set; } = string.Empty;
        public DateTime SessionDate { get; set; }
        public double BuyIn { get; set; }
        public double CashOut { get; set; }
        public double Profit => CashOut - BuyIn;
        public TimeSpan Duration { get; set; }
        public int PlayerCount { get; set; }

        public PlayerSessionSummary() { }

        public PlayerSessionSummary(string sessionId, string sessionName, DateTime sessionDate, double buyIn, double cashOut, TimeSpan duration, int playerCount)
        {
            SessionId = sessionId;
            SessionName = sessionName;
            SessionDate = sessionDate;
            BuyIn = buyIn;
            CashOut = cashOut;
            Duration = duration;
            PlayerCount = playerCount;
        }
    }
} 