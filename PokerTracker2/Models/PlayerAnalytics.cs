using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;

namespace PokerTracker2.Models
{
    /// <summary>
    /// Comprehensive analytics data for a specific player
    /// </summary>
    public class PlayerAnalytics : INotifyPropertyChanged
    {
        private string _playerName = string.Empty;
        private List<Session> _sessions = new List<Session>();
        private int _totalSessions;
        private double _totalBuyIns;
        private double _totalCashOuts;
        private double _totalProfit;
        private double _averageProfitPerSession;
        private double _bestSession;
        private double _worstSession;
        private DateTime _firstSessionDate;
        private DateTime _lastSessionDate;

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public List<Session> Sessions
        {
            get => _sessions;
            set => SetProperty(ref _sessions, value);
        }

        public int TotalSessions
        {
            get => _totalSessions;
            set => SetProperty(ref _totalSessions, value);
        }

        public double TotalBuyIns
        {
            get => _totalBuyIns;
            set => SetProperty(ref _totalBuyIns, value);
        }

        public double TotalCashOuts
        {
            get => _totalCashOuts;
            set => SetProperty(ref _totalCashOuts, value);
        }

        public double TotalProfit
        {
            get => _totalProfit;
            set => SetProperty(ref _totalProfit, value);
        }

        public double AverageProfitPerSession
        {
            get => _averageProfitPerSession;
            set => SetProperty(ref _averageProfitPerSession, value);
        }

        public double BestSession
        {
            get => _bestSession;
            set => SetProperty(ref _bestSession, value);
        }

        public double WorstSession
        {
            get => _worstSession;
            set => SetProperty(ref _worstSession, value);
        }

        public DateTime FirstSessionDate
        {
            get => _firstSessionDate;
            set => SetProperty(ref _firstSessionDate, value);
        }

        public DateTime LastSessionDate
        {
            get => _lastSessionDate;
            set => SetProperty(ref _lastSessionDate, value);
        }

        // Computed properties for analytics
        public double WinRate => TotalSessions > 0 ? Sessions.Count(s => GetPlayerProfit(s) > 0) / (double)TotalSessions * 100 : 0;
        public double LossRate => TotalSessions > 0 ? Sessions.Count(s => GetPlayerProfit(s) < 0) / (double)TotalSessions * 100 : 0;
        public double BreakEvenRate => TotalSessions > 0 ? Sessions.Count(s => GetPlayerProfit(s) == 0) / (double)TotalSessions * 100 : 0;

        public TimeSpan TotalPlayTime => Sessions
            .Where(s => s.IsCompleted)
            .Aggregate(TimeSpan.Zero, (total, session) => total + (session.EndTime - session.StartTime));

        public double AverageSessionLength => TotalSessions > 0 ? TotalPlayTime.TotalMinutes / TotalSessions : 0;

        public List<double> ProfitTrend => Sessions
            .OrderBy(s => s.StartTime)
            .Select(s => GetPlayerProfit(s))
            .ToList();

        public List<DateTime> SessionDates => Sessions
            .OrderBy(s => s.StartTime)
            .Select(s => s.StartTime)
            .ToList();

        public List<double> CumulativeProfit => Sessions
            .OrderBy(s => s.StartTime)
            .Select((s, index) => Sessions.Take(index + 1).Sum(session => GetPlayerProfit(session)))
            .ToList();

        public PlayerAnalytics() { }

        public PlayerAnalytics(string playerName)
        {
            PlayerName = playerName;
        }

        /// <summary>
        /// Get the profit/loss for a specific player in a session
        /// </summary>
        private double GetPlayerProfit(Session session)
        {
            var player = session.Players?.FirstOrDefault(p => p.Name == PlayerName);
            if (player == null) return 0;
            return player.TotalCashOut - player.TotalBuyIn;
        }

        /// <summary>
        /// Get monthly profit aggregation
        /// </summary>
        public Dictionary<string, double> GetMonthlyProfit()
        {
            return Sessions
                .GroupBy(s => new { s.StartTime.Year, s.StartTime.Month })
                .ToDictionary(
                    g => $"{g.Key.Year}-{g.Key.Month:D2}",
                    g => g.Sum(s => GetPlayerProfit(s))
                );
        }

        /// <summary>
        /// Get profit by day of week
        /// </summary>
        public Dictionary<DayOfWeek, double> GetProfitByDayOfWeek()
        {
            return Sessions
                .GroupBy(s => s.StartTime.DayOfWeek)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => GetPlayerProfit(s))
                );
        }

        /// <summary>
        /// Get profit by hour of day
        /// </summary>
        public Dictionary<int, double> GetProfitByHour()
        {
            return Sessions
                .GroupBy(s => s.StartTime.Hour)
                .ToDictionary(
                    g => g.Key,
                    g => g.Sum(s => GetPlayerProfit(s))
                );
        }

        /// <summary>
        /// Get streak information (consecutive wins/losses)
        /// </summary>
        public (int LongestWinStreak, int LongestLossStreak, int CurrentStreak) GetStreakInfo()
        {
            var profits = ProfitTrend;
            if (profits.Count == 0) return (0, 0, 0);

            var longestWinStreak = 0;
            var longestLossStreak = 0;
            var currentStreak = 0;
            var maxWinStreak = 0;
            var maxLossStreak = 0;

            for (int i = 0; i < profits.Count; i++)
            {
                if (profits[i] > 0)
                {
                    if (currentStreak > 0) // Was on a win streak
                    {
                        currentStreak++;
                        maxWinStreak = Math.Max(maxWinStreak, currentStreak);
                    }
                    else // Starting a new win streak
                    {
                        currentStreak = 1;
                        maxWinStreak = Math.Max(maxWinStreak, currentStreak);
                    }
                }
                else if (profits[i] < 0)
                {
                    if (currentStreak < 0) // Was on a loss streak
                    {
                        currentStreak--;
                        maxLossStreak = Math.Max(maxLossStreak, Math.Abs(currentStreak));
                    }
                    else // Starting a new loss streak
                    {
                        currentStreak = -1;
                        maxLossStreak = Math.Max(maxLossStreak, Math.Abs(currentStreak));
                    }
                }
                else // Break even
                {
                    currentStreak = 0;
                }
            }

            return (maxWinStreak, maxLossStreak, Math.Abs(currentStreak));
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
    }
}
