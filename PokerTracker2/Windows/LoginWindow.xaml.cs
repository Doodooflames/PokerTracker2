using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PokerTracker2.Models;
using PokerTracker2.Services;
using System.Threading.Tasks;

namespace PokerTracker2.Windows
{
    public partial class LoginWindow : Window
    {
        private readonly PlayerManager _playerManager;
        private bool _isInitialized = false;

        public LoginWindow()
        {
            try
            {
                LoggingService.Instance.Info("=== LOGIN WINDOW INITIALIZATION START ===", "LoginWindow");
                LoggingService.Instance.Info("LoginWindow constructor called", "LoginWindow");
                
                // Initialize component first
                LoggingService.Instance.Info("Calling InitializeComponent...", "LoginWindow");
                InitializeComponent();
                LoggingService.Instance.Info("InitializeComponent completed successfully", "LoginWindow");
                
                // Create PlayerManager instance (single instance per window)
                LoggingService.Instance.Info("Creating PlayerManager...", "LoginWindow");
                _playerManager = new PlayerManager();
                LoggingService.Instance.Info("PlayerManager created successfully", "LoginWindow");
                
                // Set up event handlers
                LoggingService.Instance.Info("Setting up event handlers...", "LoginWindow");
                this.MouseLeftButtonDown += Window_MouseLeftButtonDown;
                LoggingService.Instance.Info("MouseLeftButtonDown event handler attached", "LoginWindow");
                
                // Apply Aero blur effect when window is initialized
                LoggingService.Instance.Info("Setting up SourceInitialized event...", "LoginWindow");
                SourceInitialized += (s, e) => 
                {
                    try
                    {
                        LoggingService.Instance.Info("SourceInitialized event fired, calling EnableAeroBlur...", "LoginWindow");
                        EnableAeroBlur();
                        LoggingService.Instance.Info("EnableAeroBlur completed successfully", "LoginWindow");
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error("ERROR in SourceInitialized", "LoginWindow", ex);
                    }
                };
                LoggingService.Instance.Info("SourceInitialized event handler attached", "LoginWindow");
                
                // Start async initialization
                LoggingService.Instance.Info("Starting async initialization...", "LoginWindow");
                _ = InitializeAsync();
                
                LoggingService.Instance.Info("LoginWindow constructor completed successfully", "LoginWindow");
                LoggingService.Instance.Info("=== LOGIN WINDOW INITIALIZATION COMPLETE ===", "LoginWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Critical("CRITICAL ERROR in LoginWindow constructor", "LoginWindow", ex);
                
                // Try to show error in message box if possible
                try
                {
                    MessageBox.Show($"Critical error during LoginWindow initialization:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                                  "LoginWindow Error", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
                catch
                {
                    // If even MessageBox fails, just continue with logging
                    LoggingService.Instance.Error("Could not show error message box", "LoginWindow");
                }
                
                throw; // Re-throw to prevent silent failure
            }
        }

        /// <summary>
        /// Async initialization to properly load Firebase data
        /// </summary>
        private async Task InitializeAsync()
        {
            try
            {
                LoggingService.Instance.Info("Starting async initialization...", "LoginWindow");
                
                // Initialize PlayerManager first
                LoggingService.Instance.Info("Initializing PlayerManager...", "LoginWindow");
                var playerManagerSuccess = await _playerManager.InitializeAsync();
                if (!playerManagerSuccess)
                {
                    LoggingService.Instance.Error("Failed to initialize PlayerManager", "LoginWindow");
                    // Show error in UI
                    Dispatcher.Invoke(() =>
                    {
                        StatusText.Text = "Error: Failed to initialize PlayerManager";
                        LoadingPanel.Visibility = Visibility.Collapsed;
                    });
                    return;
                }
                else
                {
                    LoggingService.Instance.Info("PlayerManager initialized successfully", "LoginWindow");
                }
                
                // Now load users for dropdown
                LoggingService.Instance.Info("Loading users for dropdown...", "LoginWindow");
                await LoadUsersForDropdownAsync();
                LoggingService.Instance.Info("Users loaded for dropdown", "LoginWindow");
                
                _isInitialized = true;
                
                // Update UI to show loaded state
                Dispatcher.Invoke(() =>
                {
                    LoadingPanel.Visibility = Visibility.Collapsed;
                    UsernameComboBox.IsEnabled = true;
                    StatusText.Text = "Status: Ready";
                });
                
                LoggingService.Instance.Info("Async initialization completed successfully", "LoginWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error during async initialization", "LoginWindow", ex);
                // Show error in UI
                Dispatcher.Invoke(() =>
                {
                    StatusText.Text = $"Error: {ex.Message}";
                    LoadingPanel.Visibility = Visibility.Collapsed;
                });
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private async Task LoadUsersForDropdownAsync()
        {
            try
            {
                LoggingService.Instance.Info("Loading player profiles with passwords for login dropdown...", "LoginWindow");
                
                // Only load player profiles with passwords from Firebase
                var profilesWithPasswords = _playerManager.Players.Where(p => p.HasPassword).ToList();
                
                LoggingService.Instance.Info($"Found {profilesWithPasswords.Count} player profiles with passwords", "LoginWindow");

                // Update UI on main thread
                Dispatcher.Invoke(() =>
                {
                    // Set the dropdown to show player names directly
                    UsernameComboBox.ItemsSource = profilesWithPasswords;
                    UsernameComboBox.DisplayMemberPath = "Name";
                    UsernameComboBox.SelectedValuePath = "Name";
                    
                    if (profilesWithPasswords.Any())
                    {
                        UsernameComboBox.SelectedIndex = 0;
                        LoggingService.Instance.Info($"Login dropdown populated with {profilesWithPasswords.Count} profiles", "LoginWindow");
                    }
                    else
                    {
                        LoggingService.Instance.Warning("No player profiles with passwords found - dropdown is empty", "LoginWindow");
                    }
                });
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error loading player profiles for login: {ex.Message}", "LoginWindow", ex);
            }
        }
        
        private async void LoginButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.Info("Login button clicked", "LoginWindow");
            
            // Check if system is initialized
            if (!_isInitialized)
            {
                LoggingService.Instance.Warning("Login attempted before system initialization complete", "LoginWindow");
                MessageBox.Show("Please wait for the system to finish loading profiles.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            
            try
            {
                var username = UsernameComboBox.Text;
                var password = PasswordBox.Password;
                
                LoggingService.Instance.Info($"Attempting login for username: {username}", "LoginWindow");
                
                if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
                {
                    LoggingService.Instance.Warning("Login failed - invalid credentials (empty username or password)", "LoginWindow");
                    MessageBox.Show("Please enter both username and password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Disable login button during authentication
                LoginButton.IsEnabled = false;
                StatusText.Text = "Authenticating...";

                // Authenticate against Firebase PlayerProfile only
                var profile = _playerManager.GetPlayerByName(username);
                
                if (profile != null && profile.HasPassword && profile.VerifyPassword(password))
                {
                    LoggingService.Instance.Info($"Login successful for player profile: {profile.DisplayName}", "LoginWindow");
                    
                    // Create a User object from the PlayerProfile for the main window
                    var profileUser = new User(profile.Name, profile.Email, profile.DisplayName, UserRole.Player);
                    profileUser.SetPassword(password); // Set the password for the User object
                    
                    var mainWindow = new MainWindow(profileUser);
                    mainWindow.Show();
                    Close();
                    return;
                }

                // If we get here, authentication failed
                LoggingService.Instance.Warning("Login failed - invalid credentials", "LoginWindow");
                MessageBox.Show("Invalid username or password.", "Login Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error during login: {ex.Message}", "LoginWindow", ex);
                MessageBox.Show($"An error occurred during login: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Re-enable login button and reset status
                LoginButton.IsEnabled = true;
                StatusText.Text = "Status: Ready";
            }
        }



        private void QuickStartButton_Click(object sender, RoutedEventArgs e)
        {
            LoggingService.Instance.Info("Quick start button clicked - bypassing authentication", "LoginWindow");
            try
            {
                // Create a default user for quick start (with admin privileges for testing)
                var defaultUser = new User("quickstart", "quickstart@poker.com", "Quick Start User", UserRole.Admin);
                defaultUser.SetPassword("1234");
                
                LoggingService.Instance.Info($"Quick start successful - launching app with user: {defaultUser.DisplayName}", "LoginWindow");
                
                // Launch main window directly
                var mainWindow = new MainWindow(defaultUser);
                mainWindow.Show();
                Close();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error during quick start: {ex.Message}", "LoginWindow", ex);
                MessageBox.Show($"An error occurred during quick start: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Window_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            try
            {
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Window_MouseLeftButtonDown called, dragging window...", "LoginWindow");
                DragMove();
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Window drag completed", "LoginWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"[{DateTime.Now:HH:mm:ss.fff}] ERROR in Window_MouseLeftButtonDown: {ex.Message}", "LoginWindow", ex);
            }
        }

        private void EnableAeroBlur()
        {
            try
            {
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] EnableAeroBlur started", "LoginWindow");
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Window handle obtained: {hwnd}", "LoginWindow");
                
                // Enable blur behind window
                ACCENT_POLICY accentPolicy = new ACCENT_POLICY
                {
                    AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] ACCENT_POLICY created", "LoginWindow");

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                    pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                    cbData = Marshal.SizeOf(accentPolicy) 
                };
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] WINDOWCOMPOSITIONATTRIBDATA created", "LoginWindow");

                Marshal.StructureToPtr(accentPolicy, data.pvData, false);
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Structure marshaled to pointer", "LoginWindow");

                int result = SetWindowCompositionAttribute(hwnd, ref data);
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] SetWindowCompositionAttribute called, result: {result}", "LoginWindow");
                
                Marshal.FreeHGlobal(data.pvData);
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Memory freed", "LoginWindow");
                
                // Apply backdrop blur to specific UI elements (now removes foreground blur)
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Calling ApplyBackdropBlurToElements...", "LoginWindow");
                ApplyBackdropBlurToElements();
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] EnableAeroBlur completed successfully", "LoginWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"[{DateTime.Now:HH:mm:ss.fff}] ERROR in EnableAeroBlur: {ex.Message}", "LoginWindow", ex);
                LoggingService.Instance.Error($"[{DateTime.Now:HH:mm:ss.fff}] Stack trace: {ex.StackTrace}", "LoginWindow");
                System.Diagnostics.Debug.WriteLine($"Aero blur not available: {ex.Message}");
            }
        }

        private void ApplyBackdropBlurToElements()
        {
            try
            {
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] ApplyBackdropBlurToElements started", "LoginWindow");
                // Remove any blur effects from UI elements to keep them crisp and legible
                // Only the Windows API backdrop blur should remain for the background
                if (MainContent != null)
                {
                    LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] MainContent found, removing blur effect", "LoginWindow");
                    MainContent.Effect = null; // Remove blur effect to keep UI elements clear
                    LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] Blur effect removed from MainContent", "LoginWindow");
                }
                else
                {
                    LoggingService.Instance.Warning($"[{DateTime.Now:HH:mm:ss.fff}] WARNING: MainContent is null!", "LoginWindow");
                }
                LoggingService.Instance.Info($"[{DateTime.Now:HH:mm:ss.fff}] ApplyBackdropBlurToElements completed successfully", "LoginWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"[{DateTime.Now:HH:mm:ss.fff}] ERROR in ApplyBackdropBlurToElements: {ex.Message}", "LoginWindow", ex);
                LoggingService.Instance.Error($"[{DateTime.Now:HH:mm:ss.fff}] Stack trace: {ex.StackTrace}", "LoginWindow");
                System.Diagnostics.Debug.WriteLine($"Failed to remove blur effects: {ex.Message}");
            }
        }





        #region Event Handlers

        // Windows API structures for Aero blur (full definitions as per MainWindow.xaml.cs)
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);

        [StructLayout(LayoutKind.Sequential)]
        private struct WINDOWCOMPOSITIONATTRIBDATA
        {
            public WINDOWCOMPOSITIONATTRIB Attr;
            public IntPtr pvData;
            public int cbData; 
        }

        private enum WINDOWCOMPOSITIONATTRIB
        {
            WCA_UNDEFINED = 0,
            WCA_NCRENDERING_ENABLED = 1,
            WCA_NCRENDERING_POLICY = 2,
            WCA_TRANSITIONS_FORCEDISABLED = 3,
            WCA_ALLOW_NCPAINT = 4,
            WCA_CAPTION_BUTTON_BOUNDS = 5,
            WCA_NONCLIENT_RTL_LAYOUT = 6,
            WCA_FORCE_ICONIC_REPRESENTATION = 7,
            WCA_EXTENDED_FRAME_BOUNDS = 8,
            WCA_HAS_ICONIC_BITMAP = 9,
            WCA_THEME_ATTRIBUTES = 10,
            WCA_NCRENDERING_EXILED = 11,
            WCA_NCADORNMENTINFO = 12,
            WCA_EXCLUDED_FROM_LIVEPREVIEW = 13,
            WCA_VIDEO_OVERLAY_ACTIVE = 14,
            WCA_FORCE_ACTIVEWINDOW_APPEARANCE = 15,
            WCA_DISALLOW_PEEK = 16,
            WCA_CLOAK = 17,
            WCA_CLOAKED = 18,
            WCA_ACCENT_POLICY = 19,
            WCA_FREEZE_REPRESENTATION = 20,
            WCA_EVER_UNCLOAKED = 21,
            WCA_VISUAL_OWNER = 22,
            WCA_HOLOGRAPHIC = 23,
            WCA_EXCLUDED_FROM_DDA = 24,
            WCA_PASSIVEUPDATEMODE = 25,
            WCA_USEDARKMODECOLORS = 26,
            WCA_LAST = 27
        }

        [StructLayout(LayoutKind.Sequential)]
        private struct ACCENT_POLICY
        {
            public ACCENT_STATE AccentState;
            public uint AccentFlags;
            public uint GradientColor;
            public uint AnimationId;
        }

        private enum ACCENT_STATE
        {
            ACCENT_DISABLED = 0,
            ACCENT_ENABLE_GRADIENT = 1,
            ACCENT_ENABLE_TRANSPARENTGRADIENT = 2,
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4,
            ACCENT_ENABLE_HOSTBACKDROP = 5,
            ACCENT_INVALID_STATE = 6
        }
        #endregion
    }
}
