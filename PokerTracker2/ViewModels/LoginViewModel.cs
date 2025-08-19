using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using PokerTracker2.Services;

namespace PokerTracker2.ViewModels
{
    public class LoginViewModel : INotifyPropertyChanged
    {
        private string _username = "admin";
        private string _password = string.Empty;
        private UpdateInfo? _availableUpdate;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Password
        {
            get => _password;
            set => SetProperty(ref _password, value);
        }

        public UpdateInfo? AvailableUpdate
        {
            get => _availableUpdate;
            set => SetProperty(ref _availableUpdate, value);
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
