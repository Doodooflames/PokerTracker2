using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;
using System.Collections.Generic; // Added missing import

namespace PokerTracker2.Models
{
    public enum UserRole
    {
        Player = 0,
        Admin = 1
    }

    public class User : INotifyPropertyChanged
    {
        private string _username = string.Empty;
        private string _email = string.Empty;
        private string _displayName = string.Empty;
        private UserRole _role = UserRole.Player;
        private DateTime _createdDate;
        private DateTime _lastLoginDate;
        private bool _isActive = true;
        private string _passwordHash = string.Empty;
        private string _salt = string.Empty;

        public string Username
        {
            get => _username;
            set => SetProperty(ref _username, value);
        }

        public string Email
        {
            get => _email;
            set => SetProperty(ref _email, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public UserRole Role
        {
            get => _role;
            set => SetProperty(ref _role, value);
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set => SetProperty(ref _createdDate, value);
        }

        public DateTime LastLoginDate
        {
            get => _lastLoginDate;
            set => SetProperty(ref _lastLoginDate, value);
        }

        public bool IsActive
        {
            get => _isActive;
            set => SetProperty(ref _isActive, value);
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

        // Computed properties
        public bool IsAdmin => Role == UserRole.Admin;
        public bool IsPlayer => Role == UserRole.Player;
        public string RoleDisplayName => Role.ToString();
        public bool IsEmpty => string.IsNullOrWhiteSpace(Username);

        public User()
        {
            CreatedDate = DateTime.Now;
            LastLoginDate = DateTime.Now;
        }

        public User(string username, string email, string displayName, UserRole role = UserRole.Player) : this()
        {
            Username = username;
            Email = email;
            DisplayName = displayName;
            Role = role;
        }

        // Password management methods
        public void SetPassword(string password)
        {
            Salt = GenerateSalt();
            PasswordHash = HashPassword(password, Salt);
        }

        public bool VerifyPassword(string password)
        {
            if (string.IsNullOrEmpty(PasswordHash) || string.IsNullOrEmpty(Salt))
                return false;

            string hashedPassword = HashPassword(password, Salt);
            return PasswordHash.Equals(hashedPassword, StringComparison.OrdinalIgnoreCase);
        }

        private string GenerateSalt()
        {
            byte[] saltBytes = new byte[32];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(saltBytes);
            }
            return Convert.ToBase64String(saltBytes);
        }

        private string HashPassword(string password, string salt)
        {
            using (var sha256 = SHA256.Create())
            {
                var combinedBytes = Encoding.UTF8.GetBytes(password + salt);
                var hashBytes = sha256.ComputeHash(combinedBytes);
                return Convert.ToBase64String(hashBytes);
            }
        }

        public void UpdateLastLogin()
        {
            LastLoginDate = DateTime.Now;
        }

        public void Clear()
        {
            Username = string.Empty;
            Email = string.Empty;
            DisplayName = string.Empty;
            Role = UserRole.Player;
            CreatedDate = DateTime.MinValue;
            LastLoginDate = DateTime.MinValue;
            IsActive = true;
            PasswordHash = string.Empty;
            Salt = string.Empty;
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
            if (obj is User other)
            {
                return string.Equals(Username, other.Username, StringComparison.OrdinalIgnoreCase);
            }
            return false;
        }

        public override int GetHashCode()
        {
            return Username?.ToLower().GetHashCode() ?? 0;
        }
    }
}
