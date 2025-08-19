using System;
using System.Windows;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using PokerTracker2.Models;
using System.Collections.Generic;
using System.IO;
using PokerTracker2.Services;

namespace PokerTracker2.Dialogs
{
    public partial class PlayerProfileDialog : Window, INotifyPropertyChanged
    {
        private PlayerProfile _playerProfile;
        private bool _isEditMode;
        private PermissionService? _permissionService;



        public PlayerProfile PlayerProfile
        {
            get => _playerProfile;
            set => SetProperty(ref _playerProfile, value);
        }

        public bool IsEditMode
        {
            get => _isEditMode;
            set => SetProperty(ref _isEditMode, value);
        }

        public bool CanManageAdmin
        {
            get
            {
                if (_permissionService == null) return false;
                var (canManage, _) = _permissionService.CheckPermission("manage_admin_status");
                return canManage;
            }
        }

        public PlayerProfileDialog(PlayerProfile? playerProfile = null, PermissionService? permissionService = null)
        {
            try
            {
                LoggingService.Instance.Info("PlayerProfileDialog constructor started", "PlayerProfileDialog");
                InitializeComponent();
                LoggingService.Instance.Info("InitializeComponent completed", "PlayerProfileDialog");
                
                // Store permission service
                _permissionService = permissionService;
                
                // Add constrained drag handler to title bar
                DialogConstraints.AddConstrainedDragHandler(titleBar, this);
                
                if (playerProfile != null)
                {
                    LoggingService.Instance.Info("Edit mode - creating copy of existing player", "PlayerProfileDialog");
                    // Edit mode - create a copy to avoid modifying the original
                                         PlayerProfile = new PlayerProfile(playerProfile.Name, playerProfile.Nickname)
                     {
                         Email = playerProfile.Email,
                         Phone = playerProfile.Phone,
                         Notes = playerProfile.Notes,
                         IsActive = playerProfile.IsActive,
                         IsAdmin = playerProfile.IsAdmin,
                         HasPassword = playerProfile.HasPassword,
                         PasswordHash = playerProfile.PasswordHash,
                         Salt = playerProfile.Salt
                     };
                    IsEditMode = true;
                    Title = "Edit Player Profile";
                    LoggingService.Instance.Info($"Edit mode set for player: {playerProfile.Name}", "PlayerProfileDialog");
                }
                else
                {
                    LoggingService.Instance.Info("Create mode - creating new player profile", "PlayerProfileDialog");
                    // Create mode
                    PlayerProfile = new PlayerProfile();
                    IsEditMode = false;
                    Title = "New Player Profile";
                    LoggingService.Instance.Info("Create mode set", "PlayerProfileDialog");
                }

                DataContext = this;
                LoggingService.Instance.Info("DataContext set", "PlayerProfileDialog");
                
                // Apply permission-based UI restrictions
                ApplyPermissionRestrictions();
                
                LoggingService.Instance.Info("PlayerProfileDialog constructor completed", "PlayerProfileDialog");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("PlayerProfileDialog constructor failed", "PlayerProfileDialog", ex);
                throw;
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.Info("SaveButton_Click started", "PlayerProfileDialog");
                if (ValidateInput())
                {
                    // Handle password setting
                    HandlePasswordSetting();
                    
                    DialogResult = true;
                    Close();
                    LoggingService.Instance.Info("Save successful", "PlayerProfileDialog");
                }
                else
                {
                    LoggingService.Instance.Warning("SaveButton_Click failed due to validation errors", "PlayerProfileDialog");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("SaveButton_Click failed", "PlayerProfileDialog", ex);
                throw;
            }
        }

        private void ApplyPermissionRestrictions()
        {
            if (_permissionService != null)
            {
                // Check if user can set passwords
                var (canSetPassword, message) = _permissionService.CheckPermission("set_player_password");
                
                if (!canSetPassword)
                {
                    // Disable password fields for non-admins
                    PasswordBox.IsEnabled = false;
                    ConfirmPasswordBox.IsEnabled = false;
                    PasswordBox.ToolTip = message;
                    ConfirmPasswordBox.ToolTip = message;
                    
                    // Add visual indicator that password fields are disabled
                    PasswordBox.Opacity = 0.5;
                    ConfirmPasswordBox.Opacity = 0.5;
                    
                    LoggingService.Instance.Info($"Password fields disabled: {message}", "PlayerProfileDialog");
                }

                // Check if user can manage admin status
                var (canManageAdmin, adminMessage) = _permissionService.CheckPermission("manage_admin_status");
                if (!canManageAdmin)
                {
                    // Hide admin toggle for non-admins
                    AdminToggleSwitch.Visibility = Visibility.Collapsed;
                    LoggingService.Instance.Info("Admin toggle hidden: User cannot manage admin status", "PlayerProfileDialog");
                }
                else
                {
                    // Show admin toggle for admins
                    AdminToggleSwitch.Visibility = Visibility.Visible;
                    LoggingService.Instance.Info("Admin toggle visible: User can manage admin status", "PlayerProfileDialog");
                }
            }
        }

        private void HandlePasswordSetting()
        {
            // Check permissions first
            if (_permissionService != null)
            {
                var (canSetPassword, message) = _permissionService.CheckPermission("set_player_password");
                if (!canSetPassword)
                {
                    LoggingService.Instance.Warning($"Password setting blocked: {message}", "PlayerProfileDialog");
                    return;
                }
            }

            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            // If both password fields are empty, clear the password
            if (string.IsNullOrWhiteSpace(password) && string.IsNullOrWhiteSpace(confirmPassword))
            {
                PlayerProfile.ClearPassword();
                LoggingService.Instance.Info("Password cleared", "PlayerProfileDialog");
                return;
            }

            // If only one password field is filled, show error
            if (string.IsNullOrWhiteSpace(password) != string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Please fill in both password fields or leave both empty.", "Password Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Validate password length
            if (password.Length < 6)
            {
                MessageBox.Show("Password must be at least 6 characters long.", "Password Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                PasswordBox.Focus();
                return;
            }

            // Validate password confirmation
            if (password != confirmPassword)
            {
                MessageBox.Show("Passwords do not match. Please try again.", "Password Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                ConfirmPasswordBox.Focus();
                return;
            }

            // Set the password
            PlayerProfile.SetPassword(password);
            LoggingService.Instance.Info("Password set successfully", "PlayerProfileDialog");
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(PlayerProfile.Name))
            {
                MessageBox.Show("Please enter a player name.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (PlayerProfile.Name.Length < 2)
            {
                MessageBox.Show("Player name must be at least 2 characters long.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            if (PlayerProfile.Name.Length > 50)
            {
                MessageBox.Show("Player name cannot exceed 50 characters.", "Validation Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                NameTextBox.Focus();
                return false;
            }

            // Validate email format if provided
            if (!string.IsNullOrWhiteSpace(PlayerProfile.Email))
            {
                try
                {
                    var email = new System.Net.Mail.MailAddress(PlayerProfile.Email);
                }
                catch
                {
                    MessageBox.Show("Please enter a valid email address.", "Validation Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    EmailTextBox.Focus();
                    return false;
                }
            }

            // Validate password fields
            var password = PasswordBox.Password;
            var confirmPassword = ConfirmPasswordBox.Password;

            // If one password field is filled, both must be filled
            if (!string.IsNullOrWhiteSpace(password) != !string.IsNullOrWhiteSpace(confirmPassword))
            {
                MessageBox.Show("Please fill in both password fields or leave both empty.", "Password Error", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return false;
            }

            // If passwords are provided, validate them
            if (!string.IsNullOrWhiteSpace(password))
            {
                if (password.Length < 6)
                {
                    MessageBox.Show("Password must be at least 6 characters long.", "Password Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    PasswordBox.Focus();
                    return false;
                }

                if (password != confirmPassword)
                {
                    MessageBox.Show("Passwords do not match. Please try again.", "Password Error", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    ConfirmPasswordBox.Focus();
                    return false;
                }
            }

            return true;
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