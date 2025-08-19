using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PokerTracker2.Models
{
    public class PlayerSessionStats : INotifyPropertyChanged
    {
        private string _playerName = string.Empty;
        private int _totalSessions;
        private int _completedSessions;
        private int _activeSessions;
        private double _totalBuyIns;
        private double _totalCashOuts;
        // REMOVED: NetProfit property - poker sessions should always balance to 0
        // private double _netProfit;
        private double _averageProfit;
        private double _bestSession;
        private double _worstSession;
        private DateTime _lastPlayed;

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public int TotalSessions
        {
            get => _totalSessions;
            set => SetProperty(ref _totalSessions, value);
        }

        public int CompletedSessions
        {
            get => _completedSessions;
            set => SetProperty(ref _completedSessions, value);
        }

        public int ActiveSessions
        {
            get => _activeSessions;
            set => SetProperty(ref _activeSessions, value);
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

        // REMOVED: NetProfit property - poker sessions should always balance to 0
        // public double NetProfit
        // {
        //     get => _netProfit;
        //     set => SetProperty(ref _netProfit, value);
        // }

        public double AverageProfit
        {
            get => _averageProfit;
            set => SetProperty(ref _averageProfit, value);
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

        public DateTime LastPlayed
        {
            get => _lastPlayed;
            set => SetProperty(ref _lastPlayed, value);
        }

        // Computed properties
        // REMOVED: NetProfitText property - poker sessions should always balance to 0
        // public string NetProfitText => NetProfit.ToString("C");
        public string TotalBuyInsText => TotalBuyIns.ToString("C");
        public string TotalCashOutsText => TotalCashOuts.ToString("C");
        public string AverageProfitText => AverageProfit.ToString("C");
        public string BestSessionText => BestSession.ToString("C");
        public string WorstSessionText => WorstSession.ToString("C");
        public string LastPlayedText => LastPlayed != DateTime.MinValue ? LastPlayed.ToString("MMM dd, yyyy") : "Never";
        public bool HasPlayed => LastPlayed != DateTime.MinValue;

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
