using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PokerTracker2.Models
{
    public class Session : INotifyPropertyChanged
    {
        private string _id = string.Empty;
        private string _name = string.Empty;
        private string _notes = string.Empty;
        private DateTime _startTime;
        private DateTime _endTime;
        private DateTime _createdAt;
        private DateTime _updatedAt;
        private List<Player> _players = new();
        private List<Transaction> _transactions = new();
        private double _totalBuyIns;
        private double _totalCashOuts;
        private string _hostedBy = string.Empty; // Track who hosted the session
        private string _createdBy = string.Empty; // Track who created the session

        public string Id
        {
            get => _id;
            set => SetProperty(ref _id, value);
        }

        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public DateTime StartTime
        {
            get => _startTime;
            set => SetProperty(ref _startTime, value);
        }

        public DateTime EndTime
        {
            get => _endTime;
            set => SetProperty(ref _endTime, value);
        }

        public DateTime CreatedAt
        {
            get => _createdAt;
            set => SetProperty(ref _createdAt, value);
        }

        public DateTime UpdatedAt
        {
            get => _updatedAt;
            set => SetProperty(ref _updatedAt, value);
        }

        public List<Player> Players
        {
            get => _players;
            set => SetProperty(ref _players, value);
        }

        public List<Transaction> Transactions
        {
            get => _transactions;
            set => SetProperty(ref _transactions, value);
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

        public string HostedBy
        {
            get => _hostedBy;
            set => SetProperty(ref _hostedBy, value);
        }

        public string CreatedBy
        {
            get => _createdBy;
            set => SetProperty(ref _createdBy, value);
        }

        public Session()
        {
            Id = Guid.NewGuid().ToString();
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
            _players = new List<Player>();
            _transactions = new List<Transaction>();
        }

        public Session(string name, string hostedBy = "") : this()
        {
            Name = name;
            HostedBy = hostedBy;
            CreatedBy = hostedBy;
        }

        public Session(string name, string hostedBy, string notes) : this(name, hostedBy)
        {
            Notes = notes;
        }

        public bool IsEmpty => string.IsNullOrWhiteSpace(Name);
        public bool IsActive => EndTime == DateTime.MinValue;
        public bool IsCompleted => EndTime != DateTime.MinValue;
        public TimeSpan Duration => IsCompleted ? EndTime - StartTime : DateTime.Now - StartTime;

        public string StatusText
        {
            get
            {
                if (IsActive)
                    return "Active";
                else
                    return "Completed";
            }
        }

        public string DurationText
        {
            get
            {
                var duration = Duration;
                if (duration.TotalHours >= 1)
                    return $"{duration.Hours}h {duration.Minutes}m";
                else
                    return $"{duration.Minutes}m";
            }
        }

        public string ProfitText
        {
            get
            {
                // Poker sessions should always balance to 0
                return "$0.00";
            }
        }

        public void Clear()
        {
            Name = string.Empty;
            Notes = string.Empty;
            StartTime = DateTime.MinValue;
            EndTime = DateTime.MinValue;
            UpdatedAt = DateTime.Now;
            _players.Clear();
            _transactions.Clear();
            TotalBuyIns = 0;
            TotalCashOuts = 0;
            HostedBy = string.Empty;
            CreatedBy = string.Empty;
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
