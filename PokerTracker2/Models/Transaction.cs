using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;

namespace PokerTracker2.Models
{
    public enum TransactionType
    {
        BuyIn,
        CashOut,
        Transfer
    }

    public class Transaction : INotifyPropertyChanged
    {
        private string _playerName = string.Empty;
        private double _amount;
        private TransactionType _type;
        private DateTime _timestamp;
        private string _notes = string.Empty;

        public string PlayerName
        {
            get => _playerName;
            set => SetProperty(ref _playerName, value);
        }

        public double Amount
        {
            get => _amount;
            set => SetProperty(ref _amount, value);
        }

        public TransactionType Type
        {
            get => _type;
            set => SetProperty(ref _type, value);
        }

        public DateTime Timestamp
        {
            get => _timestamp;
            set => SetProperty(ref _timestamp, value);
        }

        public string Notes
        {
            get => _notes;
            set => SetProperty(ref _notes, value);
        }

        public bool IsBuyIn => Type == TransactionType.BuyIn;
        public bool IsCashOut => Type == TransactionType.CashOut;
        public bool IsTransfer => Type == TransactionType.Transfer;
        public bool IsEmpty => string.IsNullOrWhiteSpace(PlayerName);
        public string DisplayText => $"{PlayerName}: {Amount:C} ({Type}) - {Timestamp:HH:mm}";

        public Transaction()
        {
            Timestamp = DateTime.Now;
        }

        public Transaction(string playerName, double amount, TransactionType type) : this()
        {
            PlayerName = playerName;
            Amount = amount;
            Type = type;
        }

        public Transaction(string playerName, double amount, TransactionType type, DateTime timestamp) : this(playerName, amount, type)
        {
            Timestamp = timestamp;
        }

        public void Clear()
        {
            PlayerName = string.Empty;
            Amount = 0;
            Type = TransactionType.BuyIn;
            Timestamp = DateTime.MinValue;
            Notes = string.Empty;
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