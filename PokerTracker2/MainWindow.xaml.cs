using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Controls.Primitives;
using PokerTracker2.Services;
using PokerTracker2.Models;
using PokerTracker2.Dialogs;
using PokerTracker2.Windows;
using System;
using System.Linq;
using System.Collections.Generic;
using System.IO;
using System.Windows.Interop;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Text;

namespace PokerTracker2
{
    public enum WindowResolutionMode
    {
        Auto,        // Adaptive sizing based on screen resolution
        Small,       // 1000x800
        Medium,      // 1200x1000
        Large,       // 1400x1200
        ExtraLarge,  // 1600x1300
        Fixed1080p,  // Optimized for 1080p screens: 1400x900
        Fixed1440p,  // Optimized for 1440p screens: 1800x1150
        Fixed4K      // Optimized for 4K screens: 2400x1600
    }

    public enum TextSizeMultiplier
    {
        Tiny,        // 0.8x - Compact text for power users
        Small,       // 0.9x - Slightly smaller than default
        Normal,      // 1.0x - Default size
        Large,       // 1.2x - Easier to read
        ExtraLarge,  // 1.4x - Much easier to read
        Huge         // 1.6x - Maximum accessibility
    }

    public partial class MainWindow : Window, INotifyPropertyChanged
    {
        // Windows API for modern Aero blur (Windows 10/11)
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



        private readonly SessionManager _sessionManager;
        private readonly PlayerManager _playerManager;
        private readonly User _currentUser;
        private readonly PermissionService _permissionService;
        private readonly AutoUpdateService _autoUpdateService;
        private string _newPlayerName = string.Empty;
        private double _newBuyInAmount;
        private double _newCashOutAmount;
        private Player? _selectedCashOutPlayer;
        private string _sessionNotes = string.Empty;
        private ObservableCollection<ActivityLogEntry> _activityLog = new();
        private string _playerSearchText = string.Empty;
        private PlayerProfile? _selectedPlayerProfile;
        private WindowResolutionMode _windowResolutionMode = WindowResolutionMode.Auto;
        private TextSizeMultiplier _textSizeMultiplier = TextSizeMultiplier.Normal;



        public MainWindow(User user)
        {
            try
            {
                _currentUser = user;
                InitializeComponent();
                
                // Load preferences and apply settings
                LoadResolutionPreference();
                LoadTextSizePreference();
                ApplyWindowResolution(_windowResolutionMode);
                ApplyTextSizeMultiplier(_textSizeMultiplier);
                
                        // Set up unified logging service
        LoggingService.DebugCallback = AddDebugMessage;
        
        // Set up Firebase debug callback (will be replaced by unified logging)
        FirebaseService.DebugCallback = AddDebugMessage;
                
                        // Set up data context
        DataContext = this;
        
        // Mark debug console as ready for logging
        LoggingService.Instance.SetDebugConsoleReady();
        
        LoggingService.Instance.Info("MainWindow constructor started", "MainWindow");
        LoggingService.Instance.Info($"Current user: {_currentUser.Username} (Role: {_currentUser.Role})", "MainWindow");
                
                _playerManager = new PlayerManager();
                LoggingService.Instance.Info("PlayerManager created", "MainWindow");
                
                _sessionManager = new SessionManager(_playerManager);
                LoggingService.Instance.Info("SessionManager created", "MainWindow");
                
                _permissionService = new PermissionService(_currentUser, _sessionManager, _playerManager);
                LoggingService.Instance.Info("PermissionService created", "MainWindow");
                
                _autoUpdateService = new AutoUpdateService();
                LoggingService.Instance.Info("AutoUpdateService created", "MainWindow");
                
                // Initialize both PlayerManager and SessionManager to load data from Firebase
                _ = Task.Run(async () =>
                {
                    try
                    {
                        LoggingService.Instance.Info("Starting PlayerManager and SessionManager initialization in background task", "MainWindow");
                        
                        // Initialize PlayerManager first to load player profiles
                        var playerManagerSuccess = await _playerManager.InitializeAsync();
                        if (playerManagerSuccess)
                        {
                            LoggingService.Instance.Info("PlayerManager initialized successfully", "MainWindow");
                            LoggingService.Instance.Info($"Player profiles loaded: {_playerManager.Players.Count}", "MainWindow");
                            
                            // Trigger UI update for player-related properties
                            Dispatcher.Invoke(() =>
                            {
                                OnPropertyChanged(nameof(AllPlayers));
                                OnPropertyChanged(nameof(AvailablePlayers));
                                LoggingService.Instance.Info("Player properties updated in UI", "MainWindow");
                            });
                        }
                        else
                        {
                            LoggingService.Instance.Error("PlayerManager initialization failed", "MainWindow");
                        }
                        
                        // Initialize SessionManager to load sessions from Firebase
                        var sessionManagerSuccess = await _sessionManager.InitializeAsync();
                        if (sessionManagerSuccess)
                        {
                            LoggingService.Instance.Info("SessionManager initialized successfully", "MainWindow");
                            LoggingService.Instance.Info($"Total sessions loaded: {_sessionManager.TotalSessions}", "MainWindow");
                            LoggingService.Instance.Info($"Active sessions: {_sessionManager.ActiveSessions}", "MainWindow");
                            
                            // Update UI on the main thread
                            Dispatcher.Invoke(() =>
                            {
                                LoggingService.Instance.Info("Updating UI on main thread after Firebase data load", "MainWindow");
                                // Access properties to trigger UI updates
                                var totalSessions = _sessionManager.TotalSessions;
                                var activeSessions = _sessionManager.ActiveSessions;
                                
                                LoggingService.Instance.Info($"UI update - TotalSessions: {totalSessions}, ActiveSessions: {activeSessions}", "MainWindow");
                                
                                UpdateDashboardStats();
                                UpdateActiveSessionsPage();
                                
                                // Load pending active session if one was found during initialization
                                _sessionManager.LoadPendingActiveSession();
                                
                                LoggingService.Instance.Info("UI update completed", "MainWindow");
                            });
                        }
                        else
                        {
                            LoggingService.Instance.Error("SessionManager initialization failed", "MainWindow");
                        }
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error($"Exception during initialization: {ex.Message}", "MainWindow", ex);
                    }
                });
                
                // Bind collections to UI
                PlayersList.ItemsSource = _sessionManager.Players;
                ManagementPlayersList.ItemsSource = _sessionManager.Players;
                // Note: SessionActivityList will be populated dynamically based on current session
                LoggingService.Instance.Info("Collections bound to UI", "MainWindow");
                
                // Subscribe to property changes
                _sessionManager.PropertyChanged += SessionManager_PropertyChanged;
                LoggingService.Instance.Info("Property change subscription set", "MainWindow");
                
                // Enable Windows-native Aero blur effect
                this.SourceInitialized += MainWindow_SourceInitialized;
                LoggingService.Instance.Info("SourceInitialized event handler set", "MainWindow");
                
                // Update window title with user info
                UpdateWindowTitle();
                
                // Show dashboard by default
                ShowPage(DashboardPage);
                // UpdateDashboardStats(); // REMOVED - will be called after Firebase data loads
                
                // TEST: Show UpdateDialog on startup for demo purposes
                // ShowTestUpdateDialog(); // Commented out for now
                
                // Check for updates automatically on startup
                _ = Task.Run(async () =>
                {
                    try
                    {
                        // Wait a bit to let the app fully load first
                        await Task.Delay(3000);
                        await CheckForUpdatesOnStartupAsync();
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error($"Error during startup update check: {ex.Message}", "MainWindow", ex);
                    }
                });
                
                LoggingService.Instance.Info("MainWindow constructor completed successfully", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("MainWindow constructor failed", "MainWindow", ex);
                throw;
            }
        }

        /// <summary>
        /// Applies the specified window resolution mode
        /// </summary>
        private void ApplyWindowResolution(WindowResolutionMode mode)
        {
            try
            {
                double width, height;
                
                switch (mode)
                {
                    case WindowResolutionMode.Auto:
                        (width, height) = CalculateAdaptiveSize();
                        break;
                    case WindowResolutionMode.Small:
                        width = 1000; height = 800;
                        break;
                    case WindowResolutionMode.Medium:
                        width = 1200; height = 1000;
                        break;
                    case WindowResolutionMode.Large:
                        width = 1400; height = 1200;
                        break;
                    case WindowResolutionMode.ExtraLarge:
                        width = 1600; height = 1300;
                        break;
                    case WindowResolutionMode.Fixed1080p:
                        width = 1400; height = 900;
                        break;
                    case WindowResolutionMode.Fixed1440p:
                        width = 1800; height = 1150;
                        break;
                    case WindowResolutionMode.Fixed4K:
                        width = 2400; height = 1600;
                        break;
                    default:
                        (width, height) = CalculateAdaptiveSize();
                        break;
                }
                
                // Apply the calculated size
                this.Width = width;
                this.Height = height;
                
                LoggingService.Instance.Info($"Window resolution applied: {mode} = {width:F0}x{height:F0}", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to apply window resolution {mode}: {ex.Message}", "MainWindow", ex);
                // Fallback to reasonable defaults
                this.Width = 1000;
                this.Height = 1000;
            }
        }

        /// <summary>
        /// Calculates adaptive window size based on screen resolution
        /// </summary>
        private (double width, double height) CalculateAdaptiveSize()
        {
            // Get the working area of the primary screen (excludes taskbar)
            double screenWidth = SystemParameters.WorkArea.Width;
            double screenHeight = SystemParameters.WorkArea.Height;
            
            // Design ratios for better screen utilization while maintaining safe margins
            double designRatioWidth = 0.70;   // 70% of screen width
            double designRatioHeight = 0.80;  // 80% of screen height
            
            // Calculate target size based on current screen
            double targetWidth = screenWidth * designRatioWidth;
            double targetHeight = screenHeight * designRatioHeight;
            
            // Maintain minimum usable size (nothing smaller than 800x700)
            targetWidth = Math.Max(800, targetWidth);
            targetHeight = Math.Max(700, targetHeight);
            
            // Ensure we don't exceed 85% of screen size for safety margin
            double maxWidth = screenWidth * 0.85;
            double maxHeight = screenHeight * 0.85;
            
            targetWidth = Math.Min(targetWidth, maxWidth);
            targetHeight = Math.Min(targetHeight, maxHeight);
            
            // Maintain aspect ratio close to square (original was 1400x1400)
            // Use the smaller dimension to ensure we fit in both directions
            double size = Math.Min(targetWidth, targetHeight);
            
            LoggingService.Instance.Debug($"Adaptive size calculated: {size:F0}x{size:F0} (Screen: {screenWidth:F0}x{screenHeight:F0})", "MainWindow");
            return (size, size);
        }

        /// <summary>
        /// Loads the saved resolution preference from application settings
        /// </summary>
        private void LoadResolutionPreference()
        {
            try
            {
                var savedMode = Properties.Settings.Default.WindowResolutionMode;
                if (Enum.TryParse<WindowResolutionMode>(savedMode, out var mode))
                {
                    _windowResolutionMode = mode;
                    LoggingService.Instance.Info($"Loaded resolution preference: {mode}", "MainWindow");
                }
                else
                {
                    _windowResolutionMode = WindowResolutionMode.Auto;
                    LoggingService.Instance.Info("No valid resolution preference found, using Auto", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to load resolution preference: {ex.Message}", "MainWindow", ex);
                _windowResolutionMode = WindowResolutionMode.Auto;
            }
        }

        /// <summary>
        /// Saves the current resolution preference to application settings
        /// </summary>
        private void SaveResolutionPreference(WindowResolutionMode mode)
        {
            try
            {
                Properties.Settings.Default.WindowResolutionMode = mode.ToString();
                Properties.Settings.Default.Save();
                LoggingService.Instance.Info($"Saved resolution preference: {mode}", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to save resolution preference: {ex.Message}", "MainWindow", ex);
            }
        }

        /// <summary>
        /// Loads the saved text size preference from application settings
        /// </summary>
        private void LoadTextSizePreference()
        {
            try
            {
                var savedSize = Properties.Settings.Default.TextSizeMultiplier;
                if (Enum.TryParse<TextSizeMultiplier>(savedSize, out var size))
                {
                    _textSizeMultiplier = size;
                    LoggingService.Instance.Info($"Loaded text size preference: {size}", "MainWindow");
                }
                else
                {
                    _textSizeMultiplier = TextSizeMultiplier.Normal;
                    LoggingService.Instance.Info("No valid text size preference found, using Normal", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to load text size preference: {ex.Message}", "MainWindow", ex);
                _textSizeMultiplier = TextSizeMultiplier.Normal;
            }
        }

        /// <summary>
        /// Saves the current text size preference to application settings
        /// </summary>
        private void SaveTextSizePreference(TextSizeMultiplier size)
        {
            try
            {
                Properties.Settings.Default.TextSizeMultiplier = size.ToString();
                Properties.Settings.Default.Save();
                LoggingService.Instance.Info($"Saved text size preference: {size}", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to save text size preference: {ex.Message}", "MainWindow", ex);
            }
        }

        /// <summary>
        /// Applies the specified text size multiplier globally to the application by directly scaling all text elements
        /// </summary>
        private void ApplyTextSizeMultiplier(TextSizeMultiplier multiplier)
        {
            try
            {
                double scale = GetTextSizeMultiplierValue(multiplier);
                
                // Update the global multiplier resource
                Application.Current.Resources["GlobalTextSizeMultiplier"] = scale;
                
                // Update Session Summary specific font size resources
                Application.Current.Resources["ScaledSessionSummaryTitleFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledSessionSummaryHeaderFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledSessionSummaryDetailFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledSessionSummaryTransactionFontSize"] = 12.0 * scale;
                
                // Update Player Profile specific font size resources
                Application.Current.Resources["ScaledPlayerNameFontSize"] = 20.0 * scale;
                Application.Current.Resources["ScaledPlayerDetailFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledStatisticsTitleFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledStatisticsValueFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledPlayerListItemNameFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledPlayerListItemDetailFontSize"] = 12.0 * scale;
                
                // Update Profit Graph specific resources
                Application.Current.Resources["ScaledGraphAxisLabelFontSize"] = 10.0 * scale;
                Application.Current.Resources["ScaledGraphTitleFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledGraphYAxisWidth"] = 30.0 * scale;
                
                // Update Session Manager specific font size resources
                Application.Current.Resources["ScaledSessionTitleFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledSessionSectionTitleFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledSessionPlayerNameFontSize"] = 15.0 * scale;
                Application.Current.Resources["ScaledSessionPlayerInfoFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledSessionSmallInfoFontSize"] = 10.0 * scale;
                Application.Current.Resources["ScaledSessionLabelFontSize"] = 9.0 * scale;
                Application.Current.Resources["ScaledSessionActivityFontSize"] = 11.0 * scale;
                
                // Update Dialog specific font size resources
                Application.Current.Resources["ScaledDialogTitleFontSize"] = 18.0 * scale;
                Application.Current.Resources["ScaledDialogSectionTitleFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledDialogPlayerNameFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledDialogPlayerInfoFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledDialogSmallInfoFontSize"] = 11.0 * scale;
                Application.Current.Resources["ScaledDialogTinyInfoFontSize"] = 10.0 * scale;
                
                // Update New Session specific font size resources
                Application.Current.Resources["ScaledNewSessionTitleFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledNewSessionSectionTitleFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledNewSessionIdFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledNewSessionPlayerNameFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledNewSessionInfoFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledNewSessionStatusFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledNewSessionEmptyMessageFontSize"] = 12.0 * scale;
                
                // Update Dashboard specific font size resources
                Application.Current.Resources["ScaledDashboardTitleFontSize"] = 28.0 * scale;
                Application.Current.Resources["ScaledDashboardSubtitleFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledDashboardSectionTitleFontSize"] = 18.0 * scale;
                Application.Current.Resources["ScaledDashboardStatValueFontSize"] = 20.0 * scale;
                Application.Current.Resources["ScaledDashboardStatLabelFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledDashboardIconFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledDashboardSessionNameFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledDashboardSessionDetailFontSize"] = 11.0 * scale;
                Application.Current.Resources["ScaledDashboardSessionStatusFontSize"] = 10.0 * scale;
                Application.Current.Resources["ScaledDashboardComboBoxFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledDashboardEmptyStateFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledDashboardEmptyDescFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledDashboardDecorationFontSize"] = 48.0 * scale;
                
                // Update Settings specific font size resources
                Application.Current.Resources["ScaledSettingsTitleFontSize"] = 32.0 * scale;
                Application.Current.Resources["ScaledSettingsSectionTitleFontSize"] = 20.0 * scale;
                Application.Current.Resources["ScaledSettingsDescriptionFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledSettingsLabelFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledSettingsComboBoxFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledSettingsHelpTextFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledSettingsFutureFontSize"] = 18.0 * scale;
                
                // Update Session History specific font size resources
                Application.Current.Resources["ScaledSessionHistoryTitleFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledSessionHistorySectionTitleFontSize"] = 18.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryStatValueFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryStatLabelFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledSessionHistorySessionNameFontSize"] = 18.0 * scale;
                Application.Current.Resources["ScaledSessionHistorySessionDetailFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryFinancialValueFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryFinancialLabelFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryStatusBadgeFontSize"] = 10.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryEmptyTitleFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryEmptyDescFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledSessionHistoryEmptyIconFontSize"] = 48.0 * scale;
                
                // Update Active Sessions specific font size resources
                Application.Current.Resources["ScaledActiveSessionsTitleFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsSubtitleFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsSectionTitleFontSize"] = 18.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsSessionNameFontSize"] = 18.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsSessionDetailFontSize"] = 14.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsFinancialValueFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsFinancialLabelFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsStatusBadgeFontSize"] = 10.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsQuickStatsTitleFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsQuickStatsValueFontSize"] = 20.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsQuickStatsLabelFontSize"] = 12.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsEmptyTitleFontSize"] = 24.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsEmptyDescFontSize"] = 16.0 * scale;
                Application.Current.Resources["ScaledActiveSessionsEmptyIconFontSize"] = 48.0 * scale;
                
                // Apply scaling directly to all text elements in the window
                ApplyTextScalingToAllElements(this, scale);
                
                LoggingService.Instance.Info($"Applied text size multiplier: {multiplier} (scale: {scale:F1}x)", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to apply text size multiplier: {ex.Message}", "MainWindow", ex);
            }
        }

        /// <summary>
        /// Applies text scaling directly to all elements in the visual tree
        /// </summary>
        private void ApplyTextScalingToAllElements(DependencyObject parent, double scale)
        {
            try
            {
                if (parent == null) return;

                // Apply scaling to the current element if it has a FontSize property
                ApplyScalingToElement(parent, scale);

                // Recursively apply to all child elements
                int childCount = VisualTreeHelper.GetChildrenCount(parent);
                for (int i = 0; i < childCount; i++)
                {
                    var child = VisualTreeHelper.GetChild(parent, i);
                    ApplyTextScalingToAllElements(child, scale);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"Error applying text scaling to element: {ex.Message}", "MainWindow");
            }
        }

        /// <summary>
        /// Applies text scaling to a specific UI element
        /// </summary>
        private void ApplyScalingToElement(DependencyObject element, double scale)
        {
            try
            {
                if (element is FrameworkElement frameworkElement)
                {
                    // Store original font size in Tag if not already stored
                    var originalFontSizeKey = "OriginalFontSize";
                    
                    if (frameworkElement.Tag is not Dictionary<string, object> tagDict)
                    {
                        tagDict = new Dictionary<string, object>();
                        frameworkElement.Tag = tagDict;
                    }

                    // Get the FontSize property if the element has one
                    var fontSizeProperty = GetFontSizeProperty(element);
                    if (fontSizeProperty != null)
                    {
                        var currentFontSize = (double)element.GetValue(fontSizeProperty);
                        
                        // Store original font size if not already stored
                        if (!tagDict.ContainsKey(originalFontSizeKey))
                        {
                            tagDict[originalFontSizeKey] = currentFontSize;
                        }
                        
                        // Apply scaling using the original font size
                        var originalFontSize = (double)tagDict[originalFontSizeKey];
                        var newFontSize = originalFontSize * scale;
                        element.SetValue(fontSizeProperty, newFontSize);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"Error scaling font for element {element?.GetType().Name}: {ex.Message}", "MainWindow");
            }
        }

        /// <summary>
        /// Gets the FontSize dependency property for different element types
        /// </summary>
        private DependencyProperty GetFontSizeProperty(DependencyObject element)
        {
            return element switch
            {
                TextBlock => TextBlock.FontSizeProperty,
                Button => Button.FontSizeProperty,
                Label => Label.FontSizeProperty,
                ComboBox => ComboBox.FontSizeProperty,
                ComboBoxItem => ComboBoxItem.FontSizeProperty,
                ListBox => ListBox.FontSizeProperty,
                ListBoxItem => ListBoxItem.FontSizeProperty,
                TextBox => TextBox.FontSizeProperty,
                CheckBox => CheckBox.FontSizeProperty,
                RadioButton => RadioButton.FontSizeProperty,
                MenuItem => MenuItem.FontSizeProperty,
                Control control when control.GetType().GetProperty("FontSize") != null => Control.FontSizeProperty,
                _ => null
            };
        }

        /// <summary>
        /// Refreshes text scaling for dynamically loaded content
        /// </summary>
        public void RefreshTextScalingForNewContent(FrameworkElement element)
        {
            try
            {
                double currentScale = GetTextSizeMultiplierValue(_textSizeMultiplier);
                ApplyTextScalingToAllElements(element, currentScale);
                LoggingService.Instance.Debug($"Applied text scaling to new content: {element.GetType().Name}", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Warning($"Error applying text scaling to new content: {ex.Message}", "MainWindow");
            }
        }

        /// <summary>
        /// Gets the numeric multiplier value for a text size enum
        /// </summary>
        private double GetTextSizeMultiplierValue(TextSizeMultiplier multiplier)
        {
            return multiplier switch
            {
                TextSizeMultiplier.Tiny => 0.8,
                TextSizeMultiplier.Small => 0.9,
                TextSizeMultiplier.Normal => 1.0,
                TextSizeMultiplier.Large => 1.2,
                TextSizeMultiplier.ExtraLarge => 1.4,
                TextSizeMultiplier.Huge => 1.6,
                _ => 1.0
            };
        }



        /// <summary>
        /// Initializes the settings page UI with current values
        /// </summary>
        private void InitializeSettingsPage()
        {
            try
            {
                // Set the resolution ComboBox selection based on current resolution mode
                foreach (ComboBoxItem item in ResolutionModeComboBox.Items)
                {
                    if (item.Tag?.ToString() == _windowResolutionMode.ToString())
                    {
                        ResolutionModeComboBox.SelectedItem = item;
                        break;
                    }
                }
                
                // Set the text size ComboBox selection based on current text size mode
                foreach (ComboBoxItem item in TextSizeComboBox.Items)
                {
                    if (item.Tag?.ToString() == _textSizeMultiplier.ToString())
                    {
                        TextSizeComboBox.SelectedItem = item;
                        break;
                    }
                }
                
                LoggingService.Instance.Debug($"Settings page initialized - Resolution: {_windowResolutionMode}, Text Size: {_textSizeMultiplier}", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to initialize settings page: {ex.Message}", "MainWindow", ex);
            }
        }

        /// <summary>
        /// Handles resolution mode ComboBox selection changes
        /// </summary>
        private void ResolutionModeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (ResolutionModeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    var tagValue = selectedItem.Tag.ToString();
                    if (Enum.TryParse<WindowResolutionMode>(tagValue, out var newMode))
                    {
                        WindowResolutionMode = newMode;
                        LoggingService.Instance.Info($"Resolution mode changed to: {newMode}", "MainWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error handling resolution mode change: {ex.Message}", "MainWindow", ex);
            }
        }

        /// <summary>
        /// Handles text size ComboBox selection changes
        /// </summary>
        private void TextSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                if (TextSizeComboBox.SelectedItem is ComboBoxItem selectedItem && selectedItem.Tag != null)
                {
                    var tagValue = selectedItem.Tag.ToString();
                    if (Enum.TryParse<TextSizeMultiplier>(tagValue, out var newSize))
                    {
                        TextSizeMultiplier = newSize;
                        LoggingService.Instance.Info($"Text size changed to: {newSize}", "MainWindow");
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error handling text size change: {ex.Message}", "MainWindow", ex);
            }
        }

        // TestUpdateDialog_Click removed for release

        private void MainWindow_SourceInitialized(object sender, EventArgs e)
        {
            EnableAeroBlur();
        }

        private void EnableAeroBlur()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                
                // Enable blur behind window
                ACCENT_POLICY accentPolicy = new ACCENT_POLICY
                {
                    AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                    pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                    cbData = Marshal.SizeOf(accentPolicy)
                };

                Marshal.StructureToPtr(accentPolicy, data.pvData, false);

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.pvData);
                
                // Apply backdrop blur to specific UI elements
                ApplyBackdropBlurToElements();
            }
            catch (Exception ex)
            {
                // Log error but don't crash if Aero is not available
                System.Diagnostics.Debug.WriteLine($"Aero blur not available: {ex.Message}");
            }
        }

        private void ApplyBackdropBlurToElements()
        {
            try
            {
                // Transparency is now handled directly in XAML via gradient colors with alpha
                // The backdrop blur effect is applied to the window background and shows through
                // the semi-transparent gradient backgrounds of the UI elements
                LoggingService.Instance.Info("Backdrop blur effects applied to UI elements successfully", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Failed to apply backdrop blur effects to UI elements", "MainWindow", ex);
            }
        }

        public void ApplyDialogBlur()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                
                // Apply maximum blur effect for dialog background
                ACCENT_POLICY accentPolicy = new ACCENT_POLICY
                {
                    AccentState = ACCENT_STATE.ACCENT_ENABLE_ACRYLICBLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0x00000000, // No overlay - just pure blur effect
                    AnimationId = 0
                };

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                    pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                    cbData = Marshal.SizeOf(accentPolicy)
                };

                Marshal.StructureToPtr(accentPolicy, data.pvData, false);

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.pvData);
                
                // Apply blur effect to the entire application
                ApplyBlurToEntireApp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dialog blur not available: {ex.Message}");
            }
        }

        private void ApplyBlurToEntireApp()
        {
            try
            {
                // Apply blur effect to the entire application window
                if (this != null)
                {
                    this.Effect = new BlurEffect
                    {
                        Radius = 8,
                        RenderingBias = RenderingBias.Quality
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Entire app blur application failed: {ex.Message}");
            }
        }

        private void ApplyBlurToPanel(FrameworkElement? panel)
        {
            if (panel != null)
            {
                panel.Effect = new BlurEffect
                {
                    Radius = 8,
                    RenderingBias = RenderingBias.Quality
                };
            }
        }

        public void RemoveDialogBlur()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                
                // Restore normal blur
                ACCENT_POLICY accentPolicy = new ACCENT_POLICY
                {
                    AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                    pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                    cbData = Marshal.SizeOf(accentPolicy)
                };

                Marshal.StructureToPtr(accentPolicy, data.pvData, false);

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.pvData);
                
                // Remove blur effects from entire application
                RemoveBlurFromEntireApp();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Dialog blur removal failed: {ex.Message}");
            }
        }

        private void RemoveBlurFromEntireApp()
        {
            try
            {
                // Remove blur effect from the entire application window
                if (this != null)
                {
                    this.Effect = null;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Entire app blur removal failed: {ex.Message}");
            }
        }

        private void RemoveBlurFromPanel(FrameworkElement? panel)
        {
            if (panel != null)
            {
                panel.Effect = null;
            }
        }

            public static Border CreateMainAppStyleBorder()
    {
        return new Border
        {
            Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 0, 0, 0)),
            CornerRadius = new CornerRadius(8),
            BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
            BorderThickness = new Thickness(1),
            Effect = new DropShadowEffect
            {
                Color = System.Windows.Media.Colors.Black,
                Direction = 270,
                ShadowDepth = 8,
                Opacity = 0.5,
                BlurRadius = 16
            }
        };
    }
    
    // Helper method for constrained dialog dragging
    private static void HandleConstrainedDrag(Window dialog)
    {
        // Get the parent window bounds
        if (dialog.Owner is Window parentWindow)
        {
            var parentBounds = new Rect(parentWindow.Left, parentWindow.Top, parentWindow.Width, parentWindow.Height);
            
            // Calculate maximum allowed position to keep dialog within parent bounds
            var maxLeft = parentBounds.Right - dialog.Width;
            var maxTop = parentBounds.Bottom - dialog.Height;
            var minLeft = parentBounds.Left;
            var minTop = parentBounds.Top;
            
            // Constrain the dialog position
            if (dialog.Left < minLeft) dialog.Left = minLeft;
            if (dialog.Left > maxLeft) dialog.Left = maxLeft;
            if (dialog.Top < minTop) dialog.Top = minTop;
            if (dialog.Top > maxTop) dialog.Top = maxTop;
        }
    }

        #region Properties for Data Binding

        public string NewPlayerName
        {
            get => _newPlayerName;
            set => SetProperty(ref _newPlayerName, value);
        }

        public double NewBuyInAmount
        {
            get => _newBuyInAmount;
            set => SetProperty(ref _newBuyInAmount, value);
        }

        public double NewCashOutAmount
        {
            get => _newCashOutAmount;
            set => SetProperty(ref _newCashOutAmount, value);
        }

        public Player? SelectedCashOutPlayer
        {
            get => _selectedCashOutPlayer;
            set => SetProperty(ref _selectedCashOutPlayer, value);
        }

        public string SessionNotes
        {
            get => _sessionNotes;
            set 
            {
                if (SetProperty(ref _sessionNotes, value))
                {
                    // Auto-save session when notes change
                    if (_sessionManager.CurrentSession != null)
                    {
                        _sessionManager.CurrentSession.Notes = value;
                        _sessionManager.SaveSession();
                        AddActivityLog("Session notes updated", ActivityType.SessionSaved);
                    }
                }
            }
        }

        // Expose SessionManager properties for binding
        public double TotalBuyIn => _sessionManager.TotalBuyIn;
        public double TotalCashOut => _sessionManager.TotalCashOut;
        public bool IsSessionBalanced => _sessionManager.IsSessionBalanced;

        // Player management properties
        public string PlayerSearchText
        {
            get => _playerSearchText;
            set => SetProperty(ref _playerSearchText, value);
        }

        public PlayerProfile? SelectedPlayerProfile
        {
            get => _selectedPlayerProfile;
            set
            {
                if (SetProperty(ref _selectedPlayerProfile, value))
                {
                    // Update dashboard stats when player selection changes
                    UpdateDashboardStats();
                }
            }
        }

        public List<PlayerProfile> AvailablePlayers => _playerManager.GetActivePlayers();
        public List<PlayerProfile> AllPlayers => _playerManager.Players.ToList();

        public WindowResolutionMode WindowResolutionMode
        {
            get => _windowResolutionMode;
            set
            {
                if (SetProperty(ref _windowResolutionMode, value))
                {
                    ApplyWindowResolution(value);
                    SaveResolutionPreference(value);
                }
            }
        }

        public TextSizeMultiplier TextSizeMultiplier
        {
            get => _textSizeMultiplier;
            set
            {
                if (SetProperty(ref _textSizeMultiplier, value))
                {
                    ApplyTextSizeMultiplier(value);
                    SaveTextSizePreference(value);
                }
            }
        }

        // Session management
        public Session? CurrentSession => _sessionManager.CurrentSession;


        #endregion

        #region Navigation Events

        // Track current animation state to prevent conflicts
        private bool _isExpanded = false;
        private bool _isAnimating = false;
        private const double ExpandedWidth = 220;
        private const double CollapsedWidth = 70;
        private readonly Duration AnimationDuration = new Duration(TimeSpan.FromMilliseconds(300));

        private void AnimateSidebar(double toWidth)
        {
            // Stop any current animation cleanly
            Sidebar.BeginAnimation(Border.WidthProperty, null);
            SidebarClip.BeginAnimation(RectangleGeometry.RectProperty, null);

            var fromWidth = Sidebar.ActualWidth;

            var widthAnimation = new DoubleAnimation
            {
                From = fromWidth,
                To = toWidth,
                Duration = AnimationDuration,
                EasingFunction = new ExponentialEase { Exponent = 2 },
                FillBehavior = FillBehavior.HoldEnd
            };

            var clipAnimation = new RectAnimation
            {
                From = new Rect(0, 0, fromWidth, 1000),
                To = new Rect(0, 0, toWidth, 1000),
                Duration = AnimationDuration,
                EasingFunction = new ExponentialEase { Exponent = 2 },
                FillBehavior = FillBehavior.HoldEnd
            };

            _isAnimating = true;

            widthAnimation.Completed += (s, e) => {
                _isAnimating = false;
                Sidebar.Width = toWidth; // Ensure final state
                Sidebar.BeginAnimation(Border.WidthProperty, null); // Remove animation
            };

            clipAnimation.Completed += (s, e) => {
                SidebarClip.Rect = new Rect(0, 0, toWidth, 1000);
                SidebarClip.BeginAnimation(RectangleGeometry.RectProperty, null);
            };

            Sidebar.BeginAnimation(Border.WidthProperty, widthAnimation);
            SidebarClip.BeginAnimation(RectangleGeometry.RectProperty, clipAnimation);
        }

        private void ShowSidebarText()
        {
            DashboardText.Visibility = Visibility.Visible;
            ActiveSessionsNavText.Visibility = Visibility.Visible;
            SessionManagementText.Visibility = Visibility.Visible;
            SessionHistoryText.Visibility = Visibility.Visible;
            PlayersText.Visibility = Visibility.Visible;
            AnalyticsText.Visibility = Visibility.Visible;
            NewSessionText.Visibility = Visibility.Visible;
            SettingsText.Visibility = Visibility.Visible;
            LogoutText.Visibility = Visibility.Visible;
            DebugText.Visibility = Visibility.Visible;
        }

        private void HideSidebarText()
        {
            if (_isExpanded) return; // Don't hide if re-expanded before delay

            DashboardText.Visibility = Visibility.Collapsed;
            ActiveSessionsNavText.Visibility = Visibility.Collapsed;
            SessionManagementText.Visibility = Visibility.Collapsed;
            SessionHistoryText.Visibility = Visibility.Collapsed;
            PlayersText.Visibility = Visibility.Collapsed;
            AnalyticsText.Visibility = Visibility.Collapsed;
            NewSessionText.Visibility = Visibility.Collapsed;
            SettingsText.Visibility = Visibility.Collapsed;
            LogoutText.Visibility = Visibility.Collapsed;
            DebugText.Visibility = Visibility.Collapsed;
        }

        private void Sidebar_MouseEnter(object sender, MouseEventArgs e)
        {
            LoggingService.Instance.Debug($"Sidebar_MouseEnter: _isExpanded={_isExpanded}, _isAnimating={_isAnimating}", "NavigationPane");
            
            if (_isExpanded && !_isAnimating)
                return;

            _isExpanded = true;
            ShowSidebarText(); // Only unmask, don't rely on visibility

            AnimateSidebar(ExpandedWidth);
        }

        private void Sidebar_MouseLeave(object sender, MouseEventArgs e)
        {
            LoggingService.Instance.Debug($"Sidebar_MouseLeave: _isExpanded={_isExpanded}, _isAnimating={_isAnimating}", "NavigationPane");
            
            if (!_isExpanded && !_isAnimating)
                return;

            _isExpanded = false;
            AnimateSidebar(CollapsedWidth);

            // Hide text *after* animation completes
            // Could delay or do it in widthAnimation.Completed
            Task.Delay(AnimationDuration.TimeSpan).ContinueWith(_ =>
            {
                Dispatcher.Invoke(HideSidebarText);
            });
        }



        private void DashboardButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(DashboardPage);
            UpdateDashboardStats();
        }

        private void ActiveSessionsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(ActiveSessionsPage);
            UpdateActiveSessionsPage();
        }

        private void SessionManagementButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SessionManagementPage);
        }

        private void SessionHistoryButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SessionHistoryPage);
            UpdateSessionHistoryPage();
        }

        private async void PlayersButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(PlayersPage);
            await UpdatePlayersPage();
        }

        private void AnalyticsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(AnalyticsPage);
        }

        private async void CheckForUpdates_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                CheckForUpdatesButton.IsEnabled = false;
                CheckForUpdatesButton.Content = "🔍 Checking...";
                
                LoggingService.Instance.Info("Manual update check initiated", "MainWindow");
                
                var updateInfo = await _autoUpdateService.CheckForUpdatesAsync();
                
                if (updateInfo != null && updateInfo.IsUpdateAvailable)
                {
                    LoggingService.Instance.Info($"Update available: {updateInfo.LatestVersion}", "MainWindow");
                    
                    var updateDialog = new Dialogs.UpdateDialog(updateInfo, _autoUpdateService);
                    updateDialog.Owner = this;
                    updateDialog.ShowDialog();
                }
                else
                {
                    MessageBox.Show("You're running the latest version of PokerTracker2!", "No Updates Available", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error checking for updates: {ex.Message}", "MainWindow", ex);
                MessageBox.Show($"Error checking for updates: {ex.Message}", "Update Check Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                CheckForUpdatesButton.IsEnabled = true;
                CheckForUpdatesButton.Content = "🔍 Check for Updates";
            }
        }

        private async Task CheckForUpdatesOnStartupAsync()
        {
            try
            {
                LoggingService.Instance.Info("Automatic startup update check initiated", "MainWindow");
                
                var updateInfo = await _autoUpdateService.CheckForUpdatesAsync();
                
                if (updateInfo != null && updateInfo.IsUpdateAvailable)
                {
                    LoggingService.Instance.Info($"Startup update check: Update available - {updateInfo.LatestVersion}", "MainWindow");
                    
                    // Show our custom UpdateDialog directly (no pre-confirmation dialog)
                    await Dispatcher.InvokeAsync(() =>
                    {
                        var updateDialog = new Dialogs.UpdateDialog(updateInfo, _autoUpdateService);
                        updateDialog.Owner = this;
                        updateDialog.ShowDialog();
                    });
                }
                else
                {
                    LoggingService.Instance.Info("Startup update check: No updates available", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error during startup update check: {ex.Message}", "MainWindow", ex);
                // Don't show error dialogs on startup - just log it
            }
        }

        private async void NewSessionButton_Click(object sender, RoutedEventArgs e)
        {
            StartNewSession();
            ShowPage(NewSessionPage);
            await RefreshAvailablePlayersAsync();
        }

        private void RecentSessionsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SessionHistoryPage);
        }

        private void SettingsButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(SettingsPage);
            InitializeSettingsPage();
        }

        private void DebugButton_Click(object sender, RoutedEventArgs e)
        {
            ShowPage(DebugPage);
        }

        private void LogoutButton_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show(
                $"Are you sure you want to logout, {_currentUser?.DisplayName ?? "User"}?",
                "Confirm Logout",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question
            );

            if (result == MessageBoxResult.Yes)
            {
                // Close current window and return to login
                var loginWindow = new LoginWindow();
                loginWindow.Show();
                Close();
            }
        }

        private void ShowPage(Grid pageToShow)
        {
            // Hide all pages
            DashboardPage.Visibility = Visibility.Collapsed;
            NewSessionPage.Visibility = Visibility.Collapsed;
            ActiveSessionsPage.Visibility = Visibility.Collapsed;
            SessionManagementPage.Visibility = Visibility.Collapsed;
            SessionHistoryPage.Visibility = Visibility.Collapsed;
            PlayersPage.Visibility = Visibility.Collapsed;
            AnalyticsPage.Visibility = Visibility.Collapsed;
            SettingsPage.Visibility = Visibility.Collapsed;
            DebugPage.Visibility = Visibility.Collapsed;

            // Show the selected page
            pageToShow.Visibility = Visibility.Visible;
        }



        private void StartNewSession()
        {
            _sessionManager.CreateSessionTemplate();
            
            // Clear input fields
            NewPlayerName = string.Empty;
            NewBuyInAmount = 0;
            NewCashOutAmount = 0;
            SelectedCashOutPlayer = null;
            SessionNotes = string.Empty;
            
            // Update UI
            SessionTitleText.Text = _sessionManager.CurrentSession?.Name ?? "New Session";
            SessionIdText.Text = $"ID: {_sessionManager.CurrentSessionId}";
            StatusText.Text = "New session template - configure and save when ready";
            UpdateDashboardStats();
        }

        #endregion

        #region Active Sessions Events

        private void LoadActiveSession_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Session session)
            {
                _sessionManager.LoadSession(session);
                UpdateManagementUI();
                ShowPage(SessionManagementPage);
                AddActivityLog($"Loaded session: {session.Name}", ActivityType.SessionLoaded);
            }
        }

        private async void EndActiveSession_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Session session)
            {
                var result = MessageBox.Show($"Are you sure you want to end session '{session.Name}'?", "End Session", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Load the session first, then end it
                    _sessionManager.LoadSession(session);
                    var success = await _sessionManager.EndSession();
                    
                    if (success)
                    {
                        // Update UI
                        UpdateActiveSessionsPage();
                        UpdateDashboardStats();
                        AddActivityLog($"Ended session: {session.Name}", ActivityType.SessionEnded);
                    }
                    else
                    {
                        MessageBox.Show("Failed to end session. Please check your connection and try again.", 
                            "End Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private async void AddPlayerToActiveSession_Click(object sender, RoutedEventArgs e)
        {
            // Check if we're in Session Management (session already loaded) or Active Sessions (need to load session)
            Session? sessionToLoad = null;
            
            if (sender is Button button && button.Tag is Session session)
            {
                // Called from Active Sessions page - load the session first
                sessionToLoad = session;
                _sessionManager.LoadSession(session);
            }
            else if (_sessionManager.CurrentSession != null)
            {
                // Called from Session Management page - session is already loaded
                sessionToLoad = _sessionManager.CurrentSession;
            }
            else
            {
                MessageBox.Show("No active session found. Please load a session first.", 
                    "No Session", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Show player selection dialog
            var playerSelectionDialog = new Windows.PlayerSelectionDialog(_sessionManager);
            playerSelectionDialog.Owner = this;
            
            // Apply blur effect to main window
            ApplyDialogBlur();
            
            if (playerSelectionDialog.ShowDialog() == true)
            {
                var selectedPlayer = playerSelectionDialog.SelectedPlayer;
                var buyInAmount = playerSelectionDialog.BuyInAmount;
                
                if (selectedPlayer != null && buyInAmount > 0)
                {
                    _sessionManager.AddPlayerToActiveSession(selectedPlayer.Name, buyInAmount);
                    await _sessionManager.SaveSession(); // Auto-save after adding player
                    
                    // Update UI based on which page we're on
                    if (sessionToLoad != null && sender is Button senderButton && senderButton.Tag is Session)
                    {
                        // Called from Active Sessions page
                        UpdateActiveSessionsPage();
                    }
                    else
                    {
                        // Called from Session Management page
                        UpdateManagementUI();
                    }
                    
                    // Log activity with additional context
                    var activityMessage = playerSelectionDialog.PlayerWasCreated 
                        ? $"Created new player {selectedPlayer.Name} and added to session with ${buyInAmount} buy-in"
                        : $"Added {selectedPlayer.Name} to active session with ${buyInAmount} buy-in";
                    
                    AddActivityLog(activityMessage, ActivityType.PlayerAdded);
                    
                    MessageBox.Show($"Successfully added {selectedPlayer.Name} to the session with ${buyInAmount:C} buy-in.", 
                        "Player Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
            
            // Remove blur effect from main window
            RemoveDialogBlur();
        }

        private async void CreateNewSessionFromActive_Click(object sender, RoutedEventArgs e)
        {
            StartNewSession();
            ShowPage(NewSessionPage);
            await RefreshAvailablePlayersAsync();
        }

        private void UpdateActiveSessionsPage()
        {
            var activeSessions = _sessionManager.GetActiveSessions();
            
            if (activeSessions.Count > 0)
            {
                ActiveSessionsList.ItemsSource = activeSessions;
                ActiveSessionsList.Visibility = Visibility.Visible;
                NoActiveSessionsMessage.Visibility = Visibility.Collapsed;
                
                // Update stats
                ActiveSessionsCountText.Text = activeSessions.Count.ToString();
                ActiveSessionsTotalBuyInsText.Text = activeSessions.Sum(s => s.TotalBuyIns).ToString("C");
                ActiveSessionsTotalPlayersText.Text = activeSessions.Sum(s => s.Players?.Count ?? 0).ToString();
            }
            else
            {
                ActiveSessionsList.Visibility = Visibility.Collapsed;
                NoActiveSessionsMessage.Visibility = Visibility.Visible;
                
                // Clear stats
                ActiveSessionsCountText.Text = "0";
                ActiveSessionsTotalBuyInsText.Text = "$0.00";
                ActiveSessionsTotalPlayersText.Text = "0";
            }
        }

        #endregion

        #region Session Management Events



        private void AddBuyInButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlayerProfile == null)
            {
                MessageBox.Show("Please select a player first.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewBuyInAmount <= 0)
            {
                MessageBox.Show("Please enter a valid buy-in amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var playerName = SelectedPlayerProfile.Name;
            var buyInAmount = NewBuyInAmount;
            
            _sessionManager.AddPlayer(playerName, buyInAmount);
            
            // Only auto-save if this is an active session (not a template)
            if (_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession))
            {
                _sessionManager.SaveSession();
                AddActivityLog($"Added buy-in for {playerName}: ${buyInAmount:F2}", ActivityType.BuyIn);
            }
            else
            {
                // This is a template - just add the buy-in without saving to file
                AddActivityLog($"Added buy-in to template for {playerName}: ${buyInAmount:F2}", ActivityType.BuyIn);
            }
            
            // Clear input fields
            NewBuyInAmount = 0;
            SelectedPlayerProfile = null;
            
            // Update status text
            if (_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession))
            {
                StatusText.Text = $"Added buy-in for {playerName}";
            }
            else
            {
                StatusText.Text = $"Added buy-in for {playerName} to template - save when ready";
            }
        }

        private void AddCashOutButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedCashOutPlayer == null)
            {
                MessageBox.Show("Please select a player first.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewCashOutAmount <= 0)
            {
                MessageBox.Show("Please enter a valid cash-out amount.", "Validation Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            var playerName = SelectedCashOutPlayer.Name;
            var cashOutAmount = NewCashOutAmount;
            
            _sessionManager.AddCashOut(playerName, cashOutAmount);
            
            // Only auto-save if this is an active session (not a template)
            if (_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession))
            {
                _sessionManager.SaveSession();
                AddActivityLog($"Added cash-out for {playerName}: ${cashOutAmount:F2}", ActivityType.CashOut);
            }
            else
            {
                // This is a template - just add the cash-out without saving to file
                AddActivityLog($"Added cash-out to template for {playerName}: ${cashOutAmount:F2}", ActivityType.CashOut);
            }
            
            // Clear input fields
            NewCashOutAmount = 0;
            SelectedCashOutPlayer = null;
            
            // Update status text
            if (_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession))
            {
                StatusText.Text = $"Added cash-out for {playerName}";
            }
            else
            {
                StatusText.Text = $"Added cash-out for {playerName} to template - save when ready";
            }
        }

        private async void SaveSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionManager.CurrentSession != null)
            {
                // Check if there are any players in the session
                if (_sessionManager.Players.Count == 0)
                {
                    MessageBox.Show("Please add at least one player to the session before saving.", 
                        "No Players", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // Update session notes
                _sessionManager.CurrentSession.Notes = SessionNotes;
                
                // Save the session template to Firebase (this will add it to active sessions)
                var success = await _sessionManager.SaveSessionTemplate();
                
                if (success)
                {
                    StatusText.Text = "Session saved successfully to Firebase and added to active sessions";
                    AddActivityLog("Session saved to Firebase and activated", ActivityType.SessionSaved);
                    
                    // Update the session title to show it's now active
                    SessionTitleText.Text = _sessionManager.CurrentSession.Name;
                    
                    MessageBox.Show("Session saved successfully to Firebase and added to active sessions!", 
                        "Session Saved", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    StatusText.Text = "Failed to save session to Firebase";
                    MessageBox.Show("Failed to save session to Firebase. Please check your connection and try again.", 
                        "Save Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private async void EndSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionManager.CurrentSession == null)
            {
                MessageBox.Show("No session to end.", "No Session", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Check if this is a template or an active session
            bool isTemplate = !_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession);
            
            if (isTemplate)
            {
                // This is a template - just discard it
                var result = MessageBox.Show("This is a session template that hasn't been saved yet. Discard the template?", 
                    "Discard Template", MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    // Clear the template
                    _sessionManager.CurrentSession = null;
                    
                    // Clear UI
                    SessionTitleText.Text = "No Active Session";
                    SessionIdText.Text = "No session ID";
                    StatusText.Text = "Session template discarded";
                    SessionNotes = string.Empty;
                    
                    AddActivityLog("Session template discarded", ActivityType.SessionEnded);
                    
                    var endResult = MessageBox.Show("Template discarded. Return to dashboard?", "Template Discarded", 
                        MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (endResult == MessageBoxResult.Yes)
                    {
                        DashboardButton_Click(sender, e);
                    }
                }
            }
            else
            {
                // This is an active session - end it properly
                var result = MessageBox.Show("Are you sure you want to end this session?", "End Session", 
                    MessageBoxButton.YesNo, MessageBoxImage.Question);
                
                if (result == MessageBoxResult.Yes)
                {
                    var success = await _sessionManager.EndSession();
                    
                    if (success)
                    {
                        // Clear UI
                        SessionTitleText.Text = "No Active Session";
                        SessionIdText.Text = "No session ID";
                        StatusText.Text = "Session ended and saved to Firebase";
                        SessionNotes = string.Empty;
                        
                        var endResult = MessageBox.Show("Session ended and saved to Firebase. Return to dashboard?", "Session Ended", 
                            MessageBoxButton.YesNo, MessageBoxImage.Question);
                        
                        if (endResult == MessageBoxResult.Yes)
                        {
                            DashboardButton_Click(sender, e);
                        }
                    }
                    else
                    {
                        MessageBox.Show("Failed to end session. Please check your Firebase connection and try again.", 
                            "End Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
        }

        private void RenameSessionTemplate_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionManager.CurrentSession == null)
            {
                MessageBox.Show("No session template to rename.", "No Session", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Apply blur to main window
            ApplyDialogBlur();
            
            // Create rename dialog
            var renameDialog = new InputDialog(
                "Enter a new name for this session:",
                "Rename Session",
                _sessionManager.CurrentSession.Name);
            
            renameDialog.Owner = this;
            var result = renameDialog.ShowDialog();
            
            // Remove blur effect from main window
            RemoveDialogBlur();
            
            if (result == true && !string.IsNullOrWhiteSpace(renameDialog.Answer))
            {
                var newName = renameDialog.Answer.Trim();
                if (newName != _sessionManager.CurrentSession.Name)
                {
                    _sessionManager.RenameCurrentSession(newName);
                    SessionTitleText.Text = newName;
                    StatusText.Text = $"Session renamed to: {newName}";
                    AddActivityLog($"Renamed session template to: {newName}", ActivityType.SessionRenamed);
                }
            }
        }

        #endregion

        #region Session Management Events

        private void LoadSessionButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.Debug("LoadSessionButton_Click started", "MainWindow");
                
                var activeSessions = _sessionManager.GetActiveSessions();
                var completedSessions = _sessionManager.GetCompletedSessions();
                
                LoggingService.Instance.Debug($"Found {activeSessions.Count} active sessions and {completedSessions.Count} completed sessions", "MainWindow");
                
                if (activeSessions.Count == 0 && completedSessions.Count == 0)
                {
                    MessageBox.Show("No sessions found. Create a new session first.", "No Sessions", 
                        MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                
                // Create session selection dialog with error handling
                SessionSelectionDialog? sessionDialog = null;
                try
                {
                    sessionDialog = new SessionSelectionDialog(activeSessions, completedSessions);
                    LoggingService.Instance.Debug("SessionSelectionDialog created successfully", "MainWindow");
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Error($"Failed to create SessionSelectionDialog: {ex.Message}", "MainWindow", ex);
                    MessageBox.Show($"Failed to open session selection dialog: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Apply blur effect to main window
                ApplyDialogBlur();
                
                try
                {
                    var result = sessionDialog.ShowDialog();
                    
                    if (result == true && sessionDialog.SelectedSession != null)
                    {
                        LoggingService.Instance.Info($"Loading session: {sessionDialog.SelectedSession.Name}", "MainWindow");
                        _sessionManager.LoadSession(sessionDialog.SelectedSession);
                        
                        // Update UI
                        UpdateManagementUI();
                        AddActivityLog($"Loaded session: {sessionDialog.SelectedSession.Name}", ActivityType.SessionLoaded);
                        
                        LoggingService.Instance.Info("Session loaded successfully", "MainWindow");
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Error($"Error during session dialog interaction: {ex.Message}", "MainWindow", ex);
                    MessageBox.Show($"Error loading session: {ex.Message}", "Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    // Remove blur effect from main window
                    RemoveDialogBlur();
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Unexpected error in LoadSessionButton_Click: {ex.Message}", "MainWindow", ex);
                MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Remove blur effect if it was applied
                try
                {
                    RemoveDialogBlur();
                }
                catch
                {
                    // Ignore errors when removing blur
                }
            }
        }

        private void EndManagementSessionButton_Click(object sender, RoutedEventArgs e)
        {
            EndSessionButton_Click(sender, e);
        }

        private void RenameSessionButton_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionManager.CurrentSession == null)
            {
                MessageBox.Show("No active session to rename.", "No Session", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            // Apply blur to main window
            ApplyDialogBlur();
            
            var inputDialog = new InputDialog("Enter new session name:", "✏️ Rename Session", _sessionManager.CurrentSession.Name);
            inputDialog.Owner = this;
            
            // Remove blur when dialog closes
            inputDialog.Closed += (s, e) => RemoveDialogBlur();
            
            if (inputDialog.ShowDialog() == true)
            {
                var newName = inputDialog.Answer;
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    _sessionManager.RenameCurrentSession(newName);
                    _sessionManager.SaveSession(); // Auto-save after renaming
                    UpdateManagementUI();
                    AddActivityLog($"Renamed session to: {newName}", ActivityType.SessionRenamed);
                }
            }
        }

        private void ManagementAddBuyIn_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string playerName)
            {
                // Apply blur to main window
                ApplyDialogBlur();
                
                var inputDialog = new InputDialog($"Enter buy-in amount for {playerName}:", "💰 Add Buy-in", "0");
                inputDialog.Owner = this;
                
                // Remove blur when dialog closes
                inputDialog.Closed += (s, e) => RemoveDialogBlur();
                
                if (inputDialog.ShowDialog() == true)
                {
                    var amount = inputDialog.Answer;
                    if (double.TryParse(amount, out double amountValue) && amountValue > 0)
                    {
                        _sessionManager.AddPlayer(playerName, amountValue);
                        _sessionManager.SaveSession(); // Auto-save after buy-in
                        UpdateManagementUI();
                        AddActivityLog($"Added buy-in for {playerName}: ${amountValue}", ActivityType.BuyIn);
                    }
                }
            }
        }

        private void ManagementSetCashOut_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string playerName)
            {
                var currentBuyIn = _sessionManager.GetPlayerBuyIn(playerName);
                var currentCashOut = _sessionManager.GetPlayerCashOut(playerName);
                
                // Get current stack (use final stack if set, otherwise calculate)
                var player = _sessionManager.GetPlayers().FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
                var currentStack = player?.CurrentStack ?? (currentBuyIn - currentCashOut);
                
                // Apply blur to main window
                ApplyDialogBlur();
                
                var inputDialog = new InputDialog($"Enter partial cash-out amount for {playerName} (mid-game withdrawal):\n\nCurrent stack: ${currentStack:F2}\n\nNote: Can only cash out what's available on table.", "💵 Set Cash-out", "0");
                inputDialog.Owner = this;
                
                // Remove blur when dialog closes
                inputDialog.Closed += (s, e) => RemoveDialogBlur();
                
                if (inputDialog.ShowDialog() == true)
                {
                    var amount = inputDialog.Answer;
                    if (double.TryParse(amount, out double amountValue) && amountValue >= 0)
                    {
                        if (amountValue > currentStack)
                        {
                            MessageBox.Show($"Cannot cash out ${amountValue:F2}. {playerName} only has ${currentStack:F2} in chips on the table.", 
                                "Insufficient Chips", MessageBoxButton.OK, MessageBoxImage.Warning);
                            return;
                        }
                        
                        _sessionManager.AddCashOut(playerName, amountValue);
                        _sessionManager.SaveSession(); // Auto-save after cash-out
                        UpdateManagementUI();
                        AddActivityLog($"Partial cash-out for {playerName}: ${amountValue}", ActivityType.CashOut);
                    }
                }
            }
        }

        private void ManagementSetFinalStack_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string playerName)
            {
                // Apply blur to main window
                ApplyDialogBlur();
                
                var currentBuyIn = _sessionManager.GetPlayerBuyIn(playerName);
                var currentCashOut = _sessionManager.GetPlayerCashOut(playerName);
                var currentStack = currentBuyIn - currentCashOut;
                
                var finalStackDialog = new FinalStackDialog(playerName, currentBuyIn, currentCashOut, currentStack);
                finalStackDialog.Owner = this;
                
                // Remove blur when dialog closes
                finalStackDialog.Closed += (s, e) => RemoveDialogBlur();
                
                if (finalStackDialog.ShowDialog() == true)
                {
                    var finalStack = finalStackDialog.Answer;
                    _sessionManager.SetPlayerFinalStack(playerName, finalStack);
                    _sessionManager.SaveSession(); // Auto-save after setting final stack
                    UpdateManagementUI();
                    AddActivityLog($"Set final stack for {playerName}: ${finalStack:F2}", ActivityType.SessionEnded);
                    
                    var difference = finalStack - currentStack;
                    var message = difference == 0 
                        ? $"Final stack for {playerName} set to ${finalStack:F2} (matches expected)."
                        : difference > 0 
                            ? $"Final stack for {playerName} set to ${finalStack:F2} (gained ${difference:F2} from other players)."
                            : $"Final stack for {playerName} set to ${finalStack:F2} (lost ${Math.Abs(difference):F2} to other players).";
                    
                    MessageBox.Show(message, "Final Stack Set", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void ExpandActivityLog_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is string playerName)
            {
                var transactions = _sessionManager.GetPlayerTransactions(playerName);
                var logText = string.Join("\n", transactions.Select(t => 
                    $"• {t.Type}: ${t.Amount:F2} at {t.Timestamp:HH:mm}"));
                
                if (string.IsNullOrEmpty(logText))
                {
                    logText = "No transactions found for this player.";
                }
                
                MessageBox.Show(logText, $"Activity Log - {playerName}", 
                    MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void CalculateOptimalPayouts_Click(object sender, RoutedEventArgs e)
        {
            // TODO: Implement optimal payout calculation
            MessageBox.Show("Optimal payout calculation feature coming soon!", "Feature Preview", 
                MessageBoxButton.OK, MessageBoxImage.Information);
        }

        private void ShowSessionSummary_Click(object sender, RoutedEventArgs e)
        {
            if (_sessionManager.CurrentSession == null)
            {
                MessageBox.Show("No active session to summarize.", "No Session", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            
            var summary = $"Session: {_sessionManager.CurrentSession.Name}\n" +
                         $"Duration: {_sessionManager.CurrentSession.DurationText}\n" +
                         $"Total Buy-ins: ${_sessionManager.TotalBuyIn:F2}\n" +
                         $"Total Cash-outs: ${_sessionManager.TotalCashOut:F2}\n" +
                         $"Balance: ${(_sessionManager.TotalCashOut - _sessionManager.TotalBuyIn):F2}\n" +
                         $"Players: {_sessionManager.Players.Count}";
            
            MessageBox.Show(summary, "Session Summary", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        #endregion

        #region Helper Methods

        private double GetBuyInAmountFromDialog()
        {
            // Apply blur to main window
            ApplyDialogBlur();
            
            // Styled input dialog using WPF
            var inputDialog = new InputDialog("Enter buy-in amount:", "💰 Add Buy-in", "0.00");
            inputDialog.Owner = this;
            
            // Remove blur when dialog closes
            inputDialog.Closed += (s, e) => RemoveDialogBlur();
            
            if (inputDialog.ShowDialog() == true)
            {
                if (double.TryParse(inputDialog.Answer, out double amount) && amount > 0)
                {
                    return amount;
                }
            }
            
            return 0;
        }

        private double GetCashOutAmountFromDialog()
        {
            // Apply blur to main window
            ApplyDialogBlur();
            
            // Styled input dialog using WPF
            var inputDialog = new InputDialog("Enter cash-out amount:", "💵 Set Cash-out", "0.00");
            inputDialog.Owner = this;
            
            // Remove blur when dialog closes
            inputDialog.Closed += (s, e) => RemoveDialogBlur();
            
            if (inputDialog.ShowDialog() == true)
            {
                if (double.TryParse(inputDialog.Answer, out double amount) && amount >= 0)
                {
                    return amount;
                }
            }
            
            return -1; // Invalid amount
        }

        private void AddActivityLog(string message, ActivityType type = ActivityType.Info)
        {
            _activityLog.Insert(0, new ActivityLogEntry(message, type));
            
            // Keep only the last 50 entries
            while (_activityLog.Count > 50)
            {
                _activityLog.RemoveAt(_activityLog.Count - 1);
            }
        }

        private void UpdateManagementUI()
        {
            if (_sessionManager.CurrentSession != null)
            {
                ManagementTitleText.Text = _sessionManager.CurrentSession.Name;
                ManagementSessionInfo.Text = $"ID: {_sessionManager.CurrentSessionId} | Started: {_sessionManager.CurrentSession.StartTime:g}";
                ManagementSessionStatusText.Text = "Active session";
                ManagementSessionStatusText.Foreground = System.Windows.Media.Brushes.Green;
                
                // Update chips on table: show sum of players' current stacks
                ManagementTotalFinalStacksText.Text = $"${_sessionManager.TotalChipsOnTable:F2}";
                
                // Update session balance: Buy-ins vs (partial cash-outs + final stacks)
                var balance = _sessionManager.TotalBuyIn - (_sessionManager.TotalCashOut + _sessionManager.TotalFinalStacks);
                var balanceText = $"${balance:F2}";
                
                if (Math.Abs(balance) < 0.01)
                {
                    ManagementSessionBalanceText.Text = balanceText + " (Balanced)";
                    ManagementSessionBalanceText.Foreground = System.Windows.Media.Brushes.Green;
                }
                else if (balance > 0)
                {
                    ManagementSessionBalanceText.Text = balanceText + " (Missing - Check current stacks)";
                    ManagementSessionBalanceText.Foreground = System.Windows.Media.Brushes.Orange;
                }
                else
                {
                    ManagementSessionBalanceText.Text = balanceText + " (Excess - Check calculations)";
                    ManagementSessionBalanceText.Foreground = System.Windows.Media.Brushes.Red;
                }
                
                // Update session activity panel
                UpdateSessionActivityPanel();
            }
            else
            {
                ManagementTitleText.Text = "Session Management";
                ManagementSessionInfo.Text = "No active session";
                ManagementSessionStatusText.Text = "No active session";
                ManagementSessionStatusText.Foreground = System.Windows.Media.Brushes.Red;
                ManagementTotalFinalStacksText.Text = "$0.00";
                ManagementSessionBalanceText.Text = "$0.00";
                ManagementSessionBalanceText.Foreground = System.Windows.Media.Brushes.Gray;
                
                // Clear session activity panel
                SessionActivityList.ItemsSource = null;
            }
        }

        private void UpdateSessionActivityPanel()
        {
            if (_sessionManager.CurrentSession == null || _sessionManager.Players.Count == 0)
            {
                SessionActivityList.ItemsSource = null;
                return;
            }

            try
            {
                var sessionActivityItems = new List<SessionActivityItem>();

                // Collect all transactions from all players in the current session
                foreach (var player in _sessionManager.Players)
                {
                    if (player.TransactionHistory != null && player.TransactionHistory.Count > 0)
                    {
                        // Add enhanced transaction history items
                        foreach (var transaction in player.TransactionHistory.OrderBy(t => t.Timestamp))
                        {
                            sessionActivityItems.Add(new SessionActivityItem(transaction, player.Name));
                        }
                    }
                }

                // If no enhanced transaction history, fall back to session-level transaction log
                if (sessionActivityItems.Count == 0)
                {
                    var sessionTransactions = _sessionManager.BuyInLog.OrderBy(t => t.Timestamp).ToList();
                    foreach (var transaction in sessionTransactions)
                    {
                        sessionActivityItems.Add(new SessionActivityItem(transaction));
                    }
                }

                // Sort all items by timestamp (oldest first)
                sessionActivityItems = sessionActivityItems.OrderBy(t => t.Timestamp).ToList();

                // Update the UI
                SessionActivityList.ItemsSource = sessionActivityItems;

                LoggingService.Instance.Debug($"Updated Session Activity panel with {sessionActivityItems.Count} transactions", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error updating Session Activity panel: {ex.Message}", "MainWindow", ex);
                SessionActivityList.ItemsSource = null;
            }
        }

        private void UpdateManagementSessionBalance()
        {
            var balance = TotalBuyIn - TotalCashOut;
            ManagementSessionBalanceText.Text = $"{balance:C}";
            
            if (Math.Abs(balance) < 0.01)
            {
                ManagementSessionBalanceText.Text += " (Balanced)";
                ManagementSessionBalanceText.Foreground = Brushes.Green;
            }
            else if (balance > 0)
            {
                ManagementSessionBalanceText.Text += " (Unbalanced - More cash-outs needed)";
                ManagementSessionBalanceText.Foreground = Brushes.Orange;
            }
            else
            {
                ManagementSessionBalanceText.Text += " (Unbalanced - More buy-ins needed)";
                ManagementSessionBalanceText.Foreground = Brushes.Red;
            }
        }

        private void UpdateManagementSessionStatus()
        {
            if (_sessionManager.CurrentSession == null)
            {
                ManagementSessionStatusText.Text = "No active session";
                ManagementSessionStatusText.Foreground = Brushes.Red;
                return;
            }

            if (_sessionManager.IsSessionBalanced)
            {
                ManagementSessionStatusText.Text = "Session Balanced ✓";
                ManagementSessionStatusText.Foreground = Brushes.Green;
            }
            else
            {
                var balance = _sessionManager.TotalBuyIn - _sessionManager.TotalCurrentStacks;
                if (balance > 0)
                {
                    ManagementSessionStatusText.Text = $"Unbalanced - ${balance:F2} missing (check current stacks)";
                    ManagementSessionStatusText.Foreground = Brushes.Orange;
                }
                else
                {
                    ManagementSessionStatusText.Text = $"Unbalanced - ${Math.Abs(balance):F2} excess (check calculations)";
                    ManagementSessionStatusText.Foreground = Brushes.Red;
                }
            }
        }

        private void UpdateWindowTitle()
        {
            if (_currentUser != null)
            {
                Title = $"PokerTracker2 - {_currentUser.DisplayName} ({_currentUser.Role})";
            }
            else
            {
                Title = "PokerTracker2";
            }
        }



        private void UpdateDashboardStats()
        {
            LoggingService.Instance.Debug("UpdateDashboardStats called", "MainWindow");
            LoggingService.Instance.Debug($"SessionManager.TotalSessions: {_sessionManager.TotalSessions}", "MainWindow");
            LoggingService.Instance.Debug($"SessionManager.ActiveSessions: {_sessionManager.ActiveSessions}", "MainWindow");
            LoggingService.Instance.Debug($"SessionManager.AllSessions.Count: {_sessionManager.AllSessions.Count}", "MainWindow");
            
            // Update dashboard statistics
            TotalSessionsText.Text = _sessionManager.TotalSessions.ToString();
            ActiveSessionsText.Text = _sessionManager.ActiveSessions.ToString();
            
            // Calculate total unique players across all sessions
            var allSessions = _sessionManager.AllSessions;
            var uniquePlayers = new HashSet<string>();
            foreach (var session in allSessions)
            {
                if (session.Players != null)
                {
                    foreach (var player in session.Players)
                    {
                        uniquePlayers.Add(player.Name);
                    }
                }
            }
            TotalPlayersText.Text = uniquePlayers.Count.ToString();
            LoggingService.Instance.Debug($"Unique players calculated: {uniquePlayers.Count}", "MainWindow");
            
            // Show player-specific stats if a player is selected
            if (_selectedPlayerProfile != null)
            {
                LoggingService.Instance.Debug($"Player profile selected: {_selectedPlayerProfile.Name}", "MainWindow");
                var playerStats = _sessionManager.GetPlayerSessionStats(_selectedPlayerProfile.Name);
                
                // Update player-specific stats panel
                PlayerSessionsText.Text = playerStats.TotalSessions.ToString();
                PlayerBuyInsText.Text = playerStats.TotalBuyInsText;
                PlayerCashOutsText.Text = playerStats.TotalCashOutsText;
                // Calculate player's cumulative profit for dashboard
                var playerSessions = _sessionManager.GetPlayerSessions(_selectedPlayerProfile.Name);
                var playerCumulativeProfit = 0.0;
                
                foreach (var session in playerSessions.OrderBy(s => s.StartTime))
                {
                    var playerInSession = session.Players?.FirstOrDefault(p => 
                        string.Equals(p.Name, _selectedPlayerProfile.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (playerInSession != null)
                    {
                        var sessionProfit = (playerInSession.TotalCashOut + playerInSession.CurrentStack) - playerInSession.TotalBuyIn;
                        playerCumulativeProfit += sessionProfit;
                    }
                }
                
                // Update player net profit on dashboard
                PlayerNetProfitText.Text = playerCumulativeProfit.ToString("C");
                
                // Set profit color based on calculated value
                if (playerCumulativeProfit > 0)
                    PlayerNetProfitText.Foreground = System.Windows.Media.Brushes.Green;
                else if (playerCumulativeProfit < 0)
                    PlayerNetProfitText.Foreground = System.Windows.Media.Brushes.Red;
                else
                    PlayerNetProfitText.Foreground = System.Windows.Media.Brushes.Gray;
                
                // Update player info text
                SelectedPlayerInfo.Text = $"{_selectedPlayerProfile.DisplayName} - {playerStats.CompletedSessions} completed sessions, Last played: {playerStats.LastPlayedText}";
                
                // Show player stats panel, but keep global stats visible for comparison
                PlayerStatsPanel.Visibility = Visibility.Visible;
                // Keep global stats visible - don't hide them
                // GlobalStatsPanel.Visibility = Visibility.Visible;
                // AdditionalGlobalStatsPanel.Visibility = Visibility.Visible;
            }
            else
            {
                LoggingService.Instance.Debug("No player profile selected, showing global stats", "MainWindow");
                // Show global stats when no player is selected
                
                // Hide player stats panel, but global stats are always visible
                PlayerStatsPanel.Visibility = Visibility.Collapsed;
                // Global stats are always visible - no need to show them again
                // GlobalStatsPanel.Visibility = Visibility.Visible;
                // AdditionalGlobalStatsPanel.Visibility = Visibility.Visible;
                
                // Check if there are any sessions in the system
                if (_sessionManager.TotalSessions == 0)
                {
                    LoggingService.Instance.Debug("No sessions exist - showing zero values", "MainWindow");
                    // No sessions exist - show zero values for financial stats
                    TotalVolumeText.Text = "$0.00";
                    AvgSessionProfitText.Text = "$0.00";
                    BestSessionText.Text = "$0.00";
                    TotalBuyInsText.Text = "$0.00";
                }
                else
                {
                    LoggingService.Instance.Debug($"Sessions exist ({_sessionManager.TotalSessions}) - calculating global stats", "MainWindow");
                    // Sessions exist - calculate global stats from completed sessions
                    var completedSessions = _sessionManager.GetCompletedSessions();
                    var totalBuyIns = completedSessions.Sum(s => s.TotalBuyIns);
                    var totalCashOuts = completedSessions.Sum(s => s.TotalCashOuts);
                    // REMOVED: NetProfit calculation - poker sessions should always balance to 0
                    // var totalNetProfit = _sessionManager.TotalNetProfit;
                    
                    // Update Total Volume (sum of all buy-ins across all sessions)
                    TotalVolumeText.Text = totalBuyIns.ToString("C");
                    
                    TotalBuyInsText.Text = totalBuyIns.ToString("C");
                    // REMOVED: TotalCashOutsText reference - element doesn't exist in XAML
                    // TotalCashOutsText.Text = totalCashOuts.ToString("C");
                    // REMOVED: NetProfit display - poker sessions should always balance to 0
                    // TotalNetProfitText.Text = totalNetProfit.ToString("C");
                    
                    LoggingService.Instance.Debug($"Global stats calculated - TotalVolume: {totalBuyIns:C}, TotalBuyIns: {totalBuyIns:C}, TotalCashOuts: {totalCashOuts:C}", "MainWindow");
                }
                
                // Reset player info text
                SelectedPlayerInfo.Text = "Select a player to view their profile and statistics";
            }
            
            // Update recent sessions list
            var recentSessions = _selectedPlayerProfile != null ? 
                _sessionManager.GetPlayerRecentSessions(_selectedPlayerProfile.Name, 5) : 
                _sessionManager.GetRecentSessions(5);
            
            LoggingService.Instance.Debug($"Recent sessions count: {recentSessions.Count}", "MainWindow");
            
            if (recentSessions.Count > 0)
            {
                RecentSessionsList.ItemsSource = recentSessions;
                RecentSessionsList.Visibility = Visibility.Visible;
                NoSessionsMessage.Visibility = Visibility.Collapsed;
            }
            else
            {
                RecentSessionsList.Visibility = Visibility.Collapsed;
                NoSessionsMessage.Visibility = Visibility.Visible;
            }
            
            LoggingService.Instance.Debug("UpdateDashboardStats completed", "MainWindow");
        }

        private void UpdateSessionHistoryPage()
        {
            var completedSessions = _sessionManager.GetCompletedSessions();
            
            // Update statistics
            HistoryTotalSessionsText.Text = completedSessions.Count.ToString();
            
            // Update list
            SessionHistoryList.ItemsSource = completedSessions;
            
            // Show/hide no sessions message
            if (completedSessions.Count == 0)
            {
                SessionHistoryList.Visibility = Visibility.Collapsed;
                NoHistorySessionsMessage.Visibility = Visibility.Visible;
            }
            else
            {
                SessionHistoryList.Visibility = Visibility.Visible;
                NoHistorySessionsMessage.Visibility = Visibility.Collapsed;
            }
        }

        // Session History Event Handlers
        private void RefreshSessionHistory_Click(object sender, RoutedEventArgs e)
        {
            UpdateSessionHistoryPage();
            AddActivityLog("Session history refreshed", ActivityType.SessionHistoryRefreshed);
        }

        private void ExportSessionHistory_Click(object sender, RoutedEventArgs e)
        {
            var completedSessions = _sessionManager.GetCompletedSessions();
            if (completedSessions.Count == 0)
            {
                MessageBox.Show("No sessions to export.", "No Data", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var saveFileDialog = new Microsoft.Win32.SaveFileDialog
            {
                Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
                FileName = $"PokerSessionHistory_{DateTime.Now:yyyyMMdd}.csv"
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    using var writer = new StreamWriter(saveFileDialog.FileName);
                    // REMOVED: NetProfit column - poker sessions should always balance to 0
                    // writer.WriteLine("Date,Time,Duration,Players,Total Buy-ins,Total Cash-outs,Net Profit,Notes");
                    writer.WriteLine("Date,Time,Duration,Players,Total Buy-ins,Total Cash-outs,Notes");
                    
                    foreach (var session in completedSessions)
                    {
                        // REMOVED: NetProfit field - poker sessions should always balance to 0
                        // writer.WriteLine($"{session.StartTime:yyyy-MM-dd},{session.StartTime:HH:mm},{session.DurationText},{session.Players?.Count ?? 0},{session.TotalBuyIns},{session.TotalCashOuts},{session.NetProfit},\"{session.Notes}\"");
                        writer.WriteLine($"{session.StartTime:yyyy-MM-dd},{session.StartTime:HH:mm},{session.DurationText},{session.Players?.Count ?? 0},{session.TotalBuyIns},{session.TotalCashOuts},\"{session.Notes}\"");
                    }
                    
                    MessageBox.Show("Session history exported successfully!", "Export Complete", MessageBoxButton.OK, MessageBoxImage.Information);
                    AddActivityLog($"Exported {completedSessions.Count} sessions to {saveFileDialog.FileName}", ActivityType.SessionHistoryExported);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Error exporting session history: {ex.Message}", "Export Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void ViewSessionDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Session session)
            {
                var details = $"Session Details\n\n" +
                             $"Date: {session.StartTime:MMMM dd, yyyy}\n" +
                             $"Time: {session.StartTime:HH:mm}\n" +
                             $"Duration: {session.DurationText}\n" +
                             $"Players: {session.Players?.Count ?? 0}\n" +
                             $"Total Buy-ins: {session.TotalBuyIns:C}\n" +
                             $"Total Cash-outs: {session.TotalCashOuts:C}\n" +
                             $"Notes: {session.Notes}";
                
                MessageBox.Show(details, "Session Details", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void LoadSessionFromHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Session session)
            {
                _sessionManager.LoadSession(session);
                ShowPage(SessionManagementPage);
                UpdateManagementUI();
                AddActivityLog($"Loaded session from {session.StartTime:MMM dd, yyyy}", ActivityType.SessionLoaded);
            }
        }

        private async void DeleteSessionFromHistory_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is Session session)
            {
                var result = MessageBox.Show($"Are you sure you want to delete the session from {session.StartTime:MMM dd, yyyy}? This action cannot be undone.", 
                                           "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        // Delete from both Firebase and local collection
                        var success = await _sessionManager.DeleteSessionAsync(session);
                        
                        if (success)
                        {
                            UpdateSessionHistoryPage();
                            AddActivityLog($"Deleted session from {session.StartTime:MMM dd, yyyy}", ActivityType.SessionDeleted);
                            MessageBox.Show("Session deleted successfully from Firebase and local collection.", "Delete Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                        {
                            MessageBox.Show("Failed to delete session from Firebase. The session may still appear after restart.", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting session: {ex.Message}", "Delete Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        LoggingService.Instance.Error($"Error deleting session: {ex.Message}", "MainWindow", ex);
                    }
                }
            }
        }

        private async Task UpdatePlayersPage()
        {
            try
            {
                // Check if PlayerManager already has all profiles loaded (from initialization)
                // If not, load them now (this handles edge cases where initialization failed)
                List<PlayerProfile> allProfiles;
                if (_playerManager.Players.Count == 0)
                {
                    LoggingService.Instance.Warning("PlayerManager appears empty, loading all profiles from Firebase", "MainWindow");
                    allProfiles = await _playerManager.GetAllPlayersAsync();
                    
                    // Update PlayerManager's collection
                    _playerManager.Players.Clear();
                    foreach (var p in allProfiles)
                    {
                        _playerManager.Players.Add(p);
                    }
                    OnPropertyChanged(nameof(AllPlayers));
                }
                else
                {
                    // Use already loaded profiles from PlayerManager initialization
                    allProfiles = _playerManager.Players.ToList();
                    LoggingService.Instance.Info($"Using already loaded profiles from PlayerManager ({allProfiles.Count} profiles)", "MainWindow");
                }
                
                // Create a temporary collection for display
                var displayProfiles = new ObservableCollection<PlayerProfile>(allProfiles);
                
                // Update the UI with all profiles
                PlayerProfilesList.ItemsSource = null;
                PlayerProfilesList.ItemsSource = displayProfiles;
                
                // Refresh text scaling for the updated content
                RefreshTextScalingForNewContent(PlayerProfilesList);
                
                // Log the update for debugging
                System.Diagnostics.Debug.WriteLine($"Updated Players page with {allProfiles.Count} profiles");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to update Players page: {ex.Message}");
                LoggingService.Instance.Error($"Failed to update Players page: {ex.Message}", "MainWindow", ex);
                // Fallback to showing only what's in PlayerManager
                PlayerProfilesList.ItemsSource = null;
                PlayerProfilesList.ItemsSource = _playerManager.Players;
                
                OnPropertyChanged(nameof(AllPlayers));
            }
        }

        private void SessionManager_PropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            // Update UI when SessionManager properties change
            OnPropertyChanged(nameof(TotalBuyIn));
            OnPropertyChanged(nameof(TotalCashOut));
            OnPropertyChanged(nameof(IsSessionBalanced));
            OnPropertyChanged(nameof(CurrentSession));
            
            // Update session totals display
            UpdateManagementUI();
        }

        #endregion

        #region INotifyPropertyChanged Implementation

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

        #endregion

        // Custom Title Bar Event Handlers
        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            // If window is maximized, restore it and follow mouse
            if (this.WindowState == WindowState.Maximized)
            {
                // Get current mouse position relative to screen
                Point mousePos = e.GetPosition(this);
                Point screenPos = this.PointToScreen(mousePos);
                
                // Restore window to normal state
                this.WindowState = WindowState.Normal;
                
                // Calculate new window position to center on mouse
                double newLeft = screenPos.X - (this.ActualWidth / 2);
                double newTop = screenPos.Y - 10; // Keep title bar near mouse
                
                // Ensure window stays within screen bounds
                newLeft = Math.Max(0, Math.Min(newLeft, SystemParameters.WorkArea.Width - this.ActualWidth));
                newTop = Math.Max(0, Math.Min(newTop, SystemParameters.WorkArea.Height - this.ActualHeight));
                
                this.Left = newLeft;
                this.Top = newTop;
                
                // Start dragging
                this.DragMove();
            }
            else
            {
                this.DragMove();
            }
        }

        private void MinimizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Start minimize animation
                var storyboard = (Storyboard)FindResource("MinimizeAnimation");
                if (storyboard != null)
                {
                    storyboard.Completed += (s, args) =>
                    {
                        try
                        {
                            this.WindowState = WindowState.Minimized;
                            // Reset opacity after a short delay to ensure window state is set
                            Dispatcher.BeginInvoke(() => this.Opacity = 1.0, System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        catch (Exception ex)
                        {
                            LoggingService.Instance.Error("Error in minimize animation completed handler", "MainWindow", ex);
                            // Fallback: just minimize without animation
                            this.WindowState = WindowState.Minimized;
                        }
                    };
                    storyboard.Begin(this);
                }
                else
                {
                    // Fallback if animation not found
                    this.WindowState = WindowState.Minimized;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in MinimizeButton_Click", "MainWindow", ex);
                // Fallback: just minimize without animation
                this.WindowState = WindowState.Minimized;
            }
        }

        private void MaximizeButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (this.WindowState == WindowState.Maximized)
                {
                    // Restore to adaptive size
                    this.WindowState = WindowState.Normal;
                    LoggingService.Instance.Debug("Window restored to normal state", "MainWindow");
                }
                else
                {
                    // Maximize to full screen
                    this.WindowState = WindowState.Maximized;
                    LoggingService.Instance.Debug("Window maximized", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error in MaximizeButton_Click", "MainWindow", ex);
                // Fallback: just toggle window state without animation
                this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void ResizeBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ClickCount == 2)
            {
                try
                {
                    if (this.WindowState == WindowState.Maximized)
                    {
                        // Restore animation
                        var storyboard = (Storyboard)FindResource("RestoreAnimation");
                        if (storyboard != null)
                        {
                            storyboard.Begin(this);
                            
                            // Set window state after animation starts
                            Dispatcher.BeginInvoke(() => this.WindowState = WindowState.Normal, System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        else
                        {
                            // Fallback if animation not found
                            this.WindowState = WindowState.Normal;
                        }
                    }
                    else
                    {
                        // Maximize animation
                        var storyboard = (Storyboard)FindResource("MaximizeAnimation");
                        if (storyboard != null)
                        {
                            storyboard.Begin(this);
                            
                            // Set window state after animation starts
                            Dispatcher.BeginInvoke(() => this.WindowState = WindowState.Maximized, System.Windows.Threading.DispatcherPriority.Loaded);
                        }
                        else
                        {
                            // Fallback if animation not found
                            this.WindowState = WindowState.Maximized;
                        }
                    }
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Error("Error in ResizeBorder double-click", "MainWindow", ex);
                    // Fallback: just toggle window state without animation
                    this.WindowState = this.WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
                }
            }
            else
            {
                this.DragMove();
            }
        }

        private void ResizeBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Handle resize logic here if needed
                // For now, just allow dragging
            }
        }

        #region Player Management Events

        private async void CreateNewPlayer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.Info("CreateNewPlayer_Click started", "MainWindow");
                
                var dialog = new Dialogs.PlayerProfileDialog(null, _permissionService);
                dialog.Owner = this;
                
                // Apply blur effect to main window
                ApplyDialogBlur();
                
                if (dialog.ShowDialog() == true)
                {
                    try
                    {
                        // Use the complete PlayerProfile from the dialog to preserve password and other settings
                        var success = await _playerManager.AddPlayerAsync(dialog.PlayerProfile);
                        if (!success)
                        {
                            MessageBox.Show($"Failed to create player '{dialog.PlayerProfile.Name}'. Please try again.",
                                "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                            return;
                        }
                        
                        var newPlayer = dialog.PlayerProfile;
                        LoggingService.Instance.Info($"Player added successfully: {newPlayer.DisplayName} (HasPassword: {newPlayer.HasPassword})", "MainWindow");
                        
                        MessageBox.Show($"Player '{newPlayer.DisplayName}' created successfully!", 
                            "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                        
                        // Refresh UI if on players page
                        OnPropertyChanged(nameof(AvailablePlayers));
                        OnPropertyChanged(nameof(AllPlayers));
                        
                        // Refresh the Players page to show the new player
                        if (PlayersPage.Visibility == Visibility.Visible)
                        {
                            await UpdatePlayersPage();
                        }
                        
                        // Also refresh the available players list if on New Session page
                        if (NewSessionPage.Visibility == Visibility.Visible)
                        {
                            await RefreshAvailablePlayersAsync();
                        }
                        
                        LoggingService.Instance.Debug("UI properties updated", "MainWindow");
                    }
                    catch (InvalidOperationException ex)
                    {
                        LoggingService.Instance.Error("InvalidOperationException in player creation", "MainWindow", ex);
                        MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                    catch (Exception ex)
                    {
                        LoggingService.Instance.Error("Unexpected exception in player creation", "MainWindow", ex);
                        MessageBox.Show($"An unexpected error occurred: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
                else
                {
                    LoggingService.Instance.Info("Dialog returned false - user cancelled", "MainWindow");
                }
                
                // Remove blur effect from main window
                RemoveDialogBlur();
                LoggingService.Instance.Debug("Dialog blur removed", "MainWindow");
                
                LoggingService.Instance.Info("CreateNewPlayer_Click completed", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Exception in CreateNewPlayer_Click", "MainWindow", ex);
                MessageBox.Show($"An error occurred while creating the player: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private async void EditPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedPlayerProfile == null)
            {
                MessageBox.Show("Please select a player to edit.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

                            var dialog = new Dialogs.PlayerProfileDialog(SelectedPlayerProfile, _permissionService);
            dialog.Owner = this;
            
            // Apply blur effect to main window
            ApplyDialogBlur();
            
            if (dialog.ShowDialog() == true)
            {
                try
                {
                    await _playerManager.UpdatePlayer(dialog.PlayerProfile);
                    MessageBox.Show($"Player '{dialog.PlayerProfile.DisplayName}' updated successfully!", 
                        "Success", MessageBoxButton.OK, MessageBoxImage.Information);
                    
                    // Refresh UI
                    OnPropertyChanged(nameof(AvailablePlayers));
                    OnPropertyChanged(nameof(AllPlayers));
                    
                    // Refresh the Players page to show the updated player
                    if (PlayersPage.Visibility == Visibility.Visible)
                    {
                        await UpdatePlayersPage();
                    }
                }
                catch (InvalidOperationException ex)
                {
                    MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
            
            // Remove blur effect from main window
            RemoveDialogBlur();
        }

        private async void DeletePlayer_Click(object sender, RoutedEventArgs e)
        {
            if (PlayerProfilesList.SelectedItem is PlayerProfile selectedPlayer)
            {
                var result = MessageBox.Show($"Are you sure you want to delete player '{selectedPlayer.Name}'?\n\nThis action cannot be undone.", "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Warning);
                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        var success = await _playerManager.DeletePlayerAsync(selectedPlayer.Name);
                        if (success)
                        {
                            MessageBox.Show($"Player '{selectedPlayer.Name}' has been deleted successfully.", "Delete Successful", MessageBoxButton.OK, MessageBoxImage.Information);
                            
                            // Refresh the player list if the Players page is visible
                            if (PlayersPage.Visibility == Visibility.Visible)
                            {
                                await UpdatePlayersPage();
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Failed to delete player '{selectedPlayer.Name}'. Please check the debug output for details.", "Delete Failed", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Error deleting player: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            else
            {
                MessageBox.Show("Please select a player to delete.", "No Player Selected", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private async void FixCorruptedData_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = MessageBox.Show(
                    "This will fix corrupted session data for all players by:\n\n" +
                    "• Clearing all session references\n" +
                    "• Resetting lifetime totals to $0\n" +
                    "• Recalculating totals from actual session data\n\n" +
                    "This is needed to fix inflated session counts and incorrect totals.\n\n" +
                    "Continue?",
                    "Fix Corrupted Data",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Warning
                );

                if (result == MessageBoxResult.Yes)
                {
                    // Show progress
                    FixCorruptedDataButton.Content = "🔧 Fixing...";
                    FixCorruptedDataButton.IsEnabled = false;

                    var success = await _playerManager.FixAllCorruptedPlayerDataAsync();
                    
                    if (success)
                    {
                        MessageBox.Show(
                            "Successfully fixed corrupted data for all players!\n\n" +
                            "Session counts and totals have been reset and recalculated.",
                            "Fix Complete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Information
                        );

                        // Refresh the player list
                        if (PlayersPage.Visibility == Visibility.Visible)
                        {
                            await UpdatePlayersPage();
                        }
                    }
                    else
                    {
                        MessageBox.Show(
                            "Some players could not be fixed. Check the debug output for details.",
                            "Fix Incomplete",
                            MessageBoxButton.OK,
                            MessageBoxImage.Warning
                        );
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error fixing corrupted data: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // Reset button
                FixCorruptedDataButton.Content = "🔧 Fix Data";
                FixCorruptedDataButton.IsEnabled = true;
            }
        }

        private void SearchPlayers_TextChanged(object sender, TextChangedEventArgs e)
        {
            // This will be handled by the search functionality in the UI
            OnPropertyChanged(nameof(AvailablePlayers));
        }

        private void SelectPlayerForSession_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlayerProfile player)
            {
                // Check if we have a session (either active or template)
                if (_sessionManager.CurrentSession == null)
                {
                    MessageBox.Show("Please start a new session first.", "No Session", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                var buyInAmount = GetBuyInAmountFromDialog();
                if (buyInAmount > 0)
                {
                    _sessionManager.AddPlayer(player.Name, buyInAmount);
                    
                    // Only auto-save if this is an active session (not a template)
                    if (_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession))
                    {
                        _sessionManager.SaveSession();
                        AddActivityLog($"Added player: {player.DisplayName} with ${buyInAmount:F2} buy-in", ActivityType.PlayerAdded);
                    }
                    else
                    {
                        // This is a template - just add the player without saving to file
                        AddActivityLog($"Added player to template: {player.DisplayName} with ${buyInAmount:F2} buy-in", ActivityType.PlayerAdded);
                    }
                    
                    // Update status text
                    if (_sessionManager.GetActiveSessions().Contains(_sessionManager.CurrentSession))
                    {
                        StatusText.Text = $"Added {player.DisplayName} to active session";
                    }
                    else
                    {
                        StatusText.Text = $"Added {player.DisplayName} to session template - save when ready";
                    }
                    
                    MessageBox.Show($"Added {player.DisplayName} to the session with ${buyInAmount:F2} buy-in.", 
                        "Player Added", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private void PlayerCard_Click(object sender, MouseButtonEventArgs e)
        {
            if (sender is Border border && border.Tag is PlayerProfile player)
            {
                SelectedPlayerProfile = player;
                UpdatePlayerDetailsUI();
            }
        }

        private void ViewPlayerDetails_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is PlayerProfile player)
            {
                SelectedPlayerProfile = player;
                UpdatePlayerDetailsUI();
            }
        }

        private void UpdatePlayerDetailsUI()
        {
            if (SelectedPlayerProfile != null)
            {
                SelectedPlayerName.Text = SelectedPlayerProfile.DisplayName;
                SelectedPlayerEmail.Text = string.IsNullOrWhiteSpace(SelectedPlayerProfile.Email) ? "No email" : SelectedPlayerProfile.Email;
                SelectedPlayerPhone.Text = string.IsNullOrWhiteSpace(SelectedPlayerProfile.Phone) ? "No phone" : SelectedPlayerProfile.Phone;
                SelectedPlayerSessions.Text = SelectedPlayerProfile.TotalSessionsPlayed.ToString();
                SelectedPlayerBuyIns.Text = SelectedPlayerProfile.TotalLifetimeBuyIn.ToString("C");
                SelectedPlayerProfit.Text = SelectedPlayerProfile.LifetimeProfit.ToString("C");
                SelectedPlayerNotes.Text = string.IsNullOrWhiteSpace(SelectedPlayerProfile.Notes) ? "No notes available" : SelectedPlayerProfile.Notes;
                
                // Set profit color
                if (SelectedPlayerProfile.LifetimeProfit > 0)
                    SelectedPlayerProfit.Foreground = System.Windows.Media.Brushes.Green;
                else if (SelectedPlayerProfile.LifetimeProfit < 0)
                    SelectedPlayerProfit.Foreground = System.Windows.Media.Brushes.Red;
                else
                    SelectedPlayerProfit.Foreground = System.Windows.Media.Brushes.Gray;
                
                // Populate activity data
                PopulatePlayerActivityData();
            }
            else
            {
                SelectedPlayerName.Text = "Select a player to view details";
                SelectedPlayerEmail.Text = "";
                SelectedPlayerPhone.Text = "";
                SelectedPlayerSessions.Text = "0";
                SelectedPlayerBuyIns.Text = "$0";
                SelectedPlayerProfit.Text = "$0";
                SelectedPlayerNotes.Text = "No notes available";
                
                // Clear activity data
                ClearPlayerActivityData();
            }
        }
        
        private void PopulatePlayerActivityData()
        {
            if (SelectedPlayerProfile == null) return;
            
            try
            {
                // Get player sessions from session manager
                var playerSessions = _sessionManager.GetPlayerSessions(SelectedPlayerProfile.Name);
                
                // Build session breakdown
                var sessionBreakdown = new List<SessionBreakdownItem>();
                var transactionHistory = new List<TransactionHistoryItem>();
                var profitTrendData = new List<ProfitPoint>();
                
                var cumulativeProfit = 0.0;
                
                // Add starting point at $0 for the line graph
                if (playerSessions.Any())
                {
                    var firstSession = playerSessions.OrderBy(s => s.StartTime).First();
                    profitTrendData.Add(new ProfitPoint
                    {
                        Profit = 0.0, // Starting at $0
                        Timestamp = firstSession.StartTime,
                        SessionId = "start"
                    });
                }
                
                foreach (var session in playerSessions.OrderBy(s => s.StartTime))
                {
                    var playerInSession = session.Players?.FirstOrDefault(p => 
                        string.Equals(p.Name, SelectedPlayerProfile.Name, StringComparison.OrdinalIgnoreCase));
                    
                    if (playerInSession != null)
                    {
                        // Calculate session profit: (Cash Out + Final Stack) - Buy Ins
                        var sessionProfit = (playerInSession.TotalCashOut + playerInSession.CurrentStack) - playerInSession.TotalBuyIn;
                        
                        // Debug logging for session profit calculation
                        LoggingService.Instance.Debug($"Session '{session.Name}' profit calculation: CashOut={playerInSession.TotalCashOut:C}, FinalStack={playerInSession.CurrentStack:C}, BuyIns={playerInSession.TotalBuyIn:C}, Profit={sessionProfit:C}", "MainWindow");
                        cumulativeProfit += sessionProfit;
                        
                        // Add to session breakdown with nested transactions list
                        var breakdownItem = new SessionBreakdownItem
                        {
                            SessionName = session.Name,
                            SessionDate = session.StartTime,
                            BuyIns = playerInSession.TotalBuyIn,
                            CashOuts = playerInSession.TotalCashOut,
                            FinalStack = playerInSession.CurrentStack,
                            Profit = sessionProfit,
                            SessionId = session.Id
                        };
                        
                        // Add to profit trend data
                        profitTrendData.Add(new ProfitPoint
                        {
                            Profit = cumulativeProfit,
                            Timestamp = session.EndTime != DateTime.MinValue ? session.EndTime : session.StartTime,
                            SessionId = session.Id
                        });
                        
                        // Add transactions to history and attach per-session nested list
                        if (session.Transactions != null)
                        {
                            var playerTransactions = session.Transactions
                                .Where(t => string.Equals(t.PlayerName, SelectedPlayerProfile.Name, StringComparison.OrdinalIgnoreCase))
                                .OrderBy(t => t.Timestamp);
                            
                            foreach (var transaction in playerTransactions)
                            {
                                TransactionHistoryItem historyItem = transaction.Type switch
                                {
                                    TransactionType.BuyIn => TransactionHistoryItem.CreateBuyIn(
                                        transaction.Amount, transaction.Notes, session.Name, transaction.Timestamp),
                                    TransactionType.CashOut => TransactionHistoryItem.CreateCashOut(
                                        transaction.Amount, transaction.Notes, session.Name, transaction.Timestamp),
                                    TransactionType.Transfer => TransactionHistoryItem.CreateTransfer(
                                        transaction.Amount, transaction.Notes, session.Name, transaction.Timestamp),
                                    _ => TransactionHistoryItem.CreateBuyIn(
                                        transaction.Amount, transaction.Notes, session.Name, transaction.Timestamp)
                                };
                                
                                transactionHistory.Add(historyItem);
                                breakdownItem.Transactions.Add(historyItem);
                            }
                            
                            // Add final stack as a cash-out transaction if there's a remaining stack
                            if (playerInSession.CurrentStack > 0)
                            {
                                var finalCashOut = TransactionHistoryItem.CreateCashOut(
                                    playerInSession.CurrentStack,
                                    "Final stack cash-out",
                                    session.Name,
                                    session.EndTime != DateTime.MinValue ? session.EndTime : session.StartTime.AddHours(4) // Use end time or estimate
                                );
                                transactionHistory.Add(finalCashOut);
                                breakdownItem.Transactions.Add(finalCashOut);
                            }
                        }

                        sessionBreakdown.Add(breakdownItem);
                    }
                }
                
                // Update the UI collections
                SelectedPlayerProfile.SessionBreakdown = sessionBreakdown;
                SelectedPlayerProfile.TransactionHistory = transactionHistory;
                SelectedPlayerProfile.ProfitTrendData = profitTrendData;
                
                // Update the main statistics display with calculated session data
                var totalBuyIns = sessionBreakdown.Sum(s => s.BuyIns);
                var totalCashOuts = sessionBreakdown.Sum(s => s.CashOuts);
                
                // Update the main UI elements
                SelectedPlayerSessions.Text = sessionBreakdown.Count.ToString();
                SelectedPlayerBuyIns.Text = totalBuyIns.ToString("C");
                SelectedPlayerProfit.Text = cumulativeProfit.ToString("C");
                
                // Set profit color based on calculated value
                if (cumulativeProfit > 0)
                    SelectedPlayerProfit.Foreground = System.Windows.Media.Brushes.Green;
                else if (cumulativeProfit < 0)
                    SelectedPlayerProfit.Foreground = System.Windows.Media.Brushes.Red;
                else
                    SelectedPlayerProfit.Foreground = System.Windows.Media.Brushes.Gray;
                
                // Populate session summary list with reverse-chronological sorting (most recent first)
                var orderedBreakdown = sessionBreakdown
                    .OrderByDescending(s => s.SessionDate)
                    .ToList();
                SessionSummaryList.ItemsSource = orderedBreakdown;
                
                // Refresh text scaling for the newly loaded session summary content
                RefreshTextScalingForNewContent(SessionSummaryList);
                
                // Convert profit trend data to BuyInPoint format for the existing graph control
                var buyInData = new ObservableCollection<BuyInPoint>(profitTrendData.Select(p => new BuyInPoint 
                { 
                    Amount = p.Profit, 
                    Timestamp = p.Timestamp 
                }).ToList());
                
                PlayerProfitGraph.BuyInData = buyInData;
                
                // Debug logging to help troubleshoot
                LoggingService.Instance.Debug($"Updated player stats: Sessions={sessionBreakdown.Count}, BuyIns={totalBuyIns:C}, Profit={cumulativeProfit:C}", "MainWindow");
                LoggingService.Instance.Debug($"Profit trend data points: {profitTrendData.Count}", "MainWindow");
                foreach (var point in profitTrendData)
                {
                    LoggingService.Instance.Debug($"  Point: Profit={point.Profit:C}, Time={point.Timestamp:HH:mm}", "MainWindow");
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Error populating player activity data", "MainWindow", ex);
            }
        }
        
        private void ClearPlayerActivityData()
        {
            // Clear activity-related UI controls
            SessionSummaryList.ItemsSource = null;
            PlayerProfitGraph.BuyInData = null;
        }

        #endregion

        private async void TestFirebase_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.Info("Testing Firebase connection...", "MainWindow");
                
                var firebaseService = new FirebaseService();
                
                // Show current credentials path for debugging
                var credentialsPath = firebaseService.GetCurrentCredentialsPath();
                LoggingService.Instance.Info($"Current credentials path: {credentialsPath}", "MainWindow");
                
                // Test basic connection
                var connectionTest = await firebaseService.TestConnectionAsync();
                if (!connectionTest)
                {
                    var errorMessage = $"❌ Firebase connection test failed!\n\n" +
                                     $"Credentials path: {credentialsPath}\n" +
                                     $"Check the debug output for details.";
                    MessageBox.Show(errorMessage, "Firebase Test", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                // Test write permissions
                var writeTest = await firebaseService.TestWriteAsync();
                if (!writeTest)
                {
                    var errorMessage = $"❌ Firebase write test failed!\n\n" +
                                     $"Credentials path: {credentialsPath}\n" +
                                     $"Check the debug output for details.";
                    MessageBox.Show(errorMessage, "Firebase Test", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                
                var successMessage = $"✅ Firebase connection successful!\n\n" +
                                   $"Credentials path: {credentialsPath}\n" +
                                   $"Both read and write operations are working.";
                MessageBox.Show(successMessage, "Firebase Test", MessageBoxButton.OK, MessageBoxImage.Information);
                
                LoggingService.Instance.Info("Firebase connection test successful", "MainWindow");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error("Firebase test failed", "MainWindow", ex);
                MessageBox.Show($"❌ Firebase test failed with exception:\n\n{ex.Message}", 
                    "Firebase Test", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }



        private void ClearDebug_Click(object sender, RoutedEventArgs e)
        {
            DebugConsoleText.Text = "🐛 Debug Console Cleared...\n📡 Ready for new debug information...\n";
            DebugConsoleScrollViewer.ScrollToBottom();
        }

        private async void TestFirebaseDebug_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                AddDebugMessage("🔗 Testing Firebase connection...");
                
                var firebaseService = new FirebaseService();
                
                // Test basic connection
                var connectionTest = await firebaseService.TestConnectionAsync();
                if (!connectionTest)
                {
                    AddDebugMessage("❌ Firebase connection test failed!");
                    AddDebugMessage("Check the debug output for details.");
                    return;
                }
                
                AddDebugMessage("✅ Firebase connection successful!");
                
                // Test write permissions
                var writeTest = await firebaseService.TestWriteAsync();
                if (!writeTest)
                {
                    AddDebugMessage("❌ Firebase write test failed!");
                    AddDebugMessage("Check the debug output for details.");
                    return;
                }
                
                AddDebugMessage("✅ Firebase write permissions working!");
                
                // Get database structure
                var dbStructure = await GetDatabaseStructureDebugAsync(firebaseService);
                AddDebugMessage("📊 Database Structure Analysis Complete!");
                AddDebugMessage(dbStructure);
                
                DebugStatusText.Text = "Firebase test completed successfully";
            }
            catch (Exception ex)
            {
                AddDebugMessage($"❌ Firebase test failed with exception: {ex.Message}");
                DebugStatusText.Text = "Firebase test failed";
            }
        }

        private void AddDebugMessage(string message)
        {
            try
            {
                var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                var logEntry = $"[{timestamp}] {message}\n";
                
                DebugConsoleText.Text += logEntry;
                DebugConsoleScrollViewer.ScrollToBottom();
                
                // Also log to debug output for traditional debugging
                System.Diagnostics.Debug.WriteLine($"DEBUG: {message}");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to add debug message: {ex.Message}");
            }
        }

        private async Task<string> GetDatabaseStructureDebugAsync(FirebaseService firebaseService)
        {
            try
            {
                var structure = new System.Text.StringBuilder();
                structure.AppendLine("🔍 Analyzing Firebase database structure...");
                
                // Test player_profiles collection
                try
                {
                    var profiles = await firebaseService.GetAllPlayerProfilesAsync();
                    structure.AppendLine($"📁 player_profiles collection: Found {profiles.Count} profiles");
                    if (profiles.Count > 0)
                    {
                        var sample = profiles.First();
                        structure.AppendLine($"   • Sample fields: {string.Join(", ", GetFieldNames(sample))}");
                    }
                }
                catch (Exception ex)
                {
                    structure.AppendLine($"❌ player_profiles collection error: {ex.Message}");
                }
                
                // Test sessions collection
                try
                {
                    var sessions = await firebaseService.GetRecentSessionsAsync(365); // Last year
                    structure.AppendLine($"📁 sessions collection: Found {sessions.Count} sessions in last year");
                    if (sessions.Count > 0)
                    {
                        var sample = sessions.First();
                        structure.AppendLine($"   • Sample fields: {string.Join(", ", GetFieldNames(sample))}");
                    }
                }
                catch (Exception ex)
                {
                    structure.AppendLine($"❌ sessions collection error: {ex.Message}");
                }
                
                structure.AppendLine("🏗️ Database is ready for use!");
                return structure.ToString();
            }
            catch (Exception ex)
            {
                return $"❌ Failed to analyze database structure: {ex.Message}";
            }
        }

        private List<string> GetFieldNames(object obj)
        {
            try
            {
                var properties = obj.GetType().GetProperties();
                return properties.Select(p => p.Name).ToList();
            }
            catch
            {
                return new List<string> { "Unable to determine fields" };
            }
        }

        private async Task RefreshAvailablePlayersAsync()
        {
            try
            {
                // Load all players from Firebase for the New Session page
                var allPlayers = await _playerManager.GetAllPlayersAsync();
                
                // Log the refresh for debugging
                System.Diagnostics.Debug.WriteLine($"Refreshed available players list with {allPlayers.Count} players from Firebase");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to refresh available players: {ex.Message}");
            }
        }

        private async void AddPlayerToNewSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                LoggingService.Instance.Info("AddPlayerToNewSession_Click called", "MainWindow");
                
                // Apply blur effect to main window
                ApplyDialogBlur();
                
                var playerSelectionDialog = new Windows.PlayerSelectionDialog(_sessionManager);
                playerSelectionDialog.Owner = this;
                
                if (playerSelectionDialog.ShowDialog() == true)
                {
                    var selectedPlayer = playerSelectionDialog.SelectedPlayer;
                    var buyInAmount = playerSelectionDialog.BuyInAmount;
                    
                    if (selectedPlayer != null && buyInAmount > 0)
                    {
                        // Add player to session
                        _sessionManager.AddPlayer(selectedPlayer.Name, buyInAmount);
                        
                        // Update UI
                        UpdateNewSessionUI();
                        
                        // Auto-save after adding player
                        var success = await _sessionManager.SaveSession();
                        if (success)
                        {
                            StatusText.Text = $"Added {selectedPlayer.Name} to session with ${buyInAmount:C} buy-in and saved to Firebase";
                        }
                        else
                        {
                            StatusText.Text = $"Added {selectedPlayer.Name} to session but failed to save to Firebase";
                        }
                        
                        // Log activity with additional context
                        var activityMessage = playerSelectionDialog.PlayerWasCreated 
                            ? $"Created new player {selectedPlayer.Name} and added to session with ${buyInAmount} buy-in"
                            : $"Added {selectedPlayer.Name} to session with ${buyInAmount} buy-in";
                        
                        AddActivityLog(activityMessage, ActivityType.PlayerAdded);
                    }
                }
                
                // Remove blur effect from main window
                RemoveDialogBlur();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error in AddPlayerToNewSession_Click: {ex.Message}", "MainWindow", ex);
                MessageBox.Show($"Error adding player to session: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                RemoveDialogBlur();
            }
        }

        private async void RemovePlayerFromSession_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (sender is Button button && button.Tag is string playerName)
                {
                    var result = MessageBox.Show($"Are you sure you want to remove {playerName} from the session?", 
                        "Remove Player", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    
                    if (result == MessageBoxResult.Yes)
                    {
                        // Remove player from session
                        _sessionManager.RemovePlayer(playerName);
                        
                        // Update UI
                        UpdateNewSessionUI();
                        
                        // Auto-save after removing player
                        var success = await _sessionManager.SaveSession();
                        if (success)
                        {
                            StatusText.Text = $"Removed {playerName} from session and saved to Firebase";
                        }
                        else
                        {
                            StatusText.Text = $"Removed {playerName} from session but failed to save to Firebase";
                        }
                        
                        AddActivityLog($"Removed player from session: {playerName}", ActivityType.PlayerRemoved);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error in RemovePlayerFromSession_Click: {ex.Message}", "MainWindow", ex);
                MessageBox.Show($"Error removing player from session: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private async void AddPlayerToSession()
        {
            if (string.IsNullOrWhiteSpace(NewPlayerName))
            {
                MessageBox.Show("Please enter a player name.", "No Player Name", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            if (NewBuyInAmount <= 0)
            {
                MessageBox.Show("Please enter a valid buy-in amount.", "Invalid Buy-in", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            try
            {
                // Store values before clearing
                var playerName = NewPlayerName;
                var buyInAmount = NewBuyInAmount;
                
                // Add player to session
                _sessionManager.AddPlayer(playerName, buyInAmount);
                
                // Clear input fields
                NewPlayerName = string.Empty;
                NewBuyInAmount = 0;
                
                // Update UI
                UpdateNewSessionUI();
                
                // Auto-save after adding player
                var success = await _sessionManager.SaveSession();
                if (success)
                {
                    StatusText.Text = $"Added {playerName} to session with ${buyInAmount:F2} buy-in and saved to Firebase";
                }
                else
                {
                    StatusText.Text = $"Added {playerName} to session but failed to save to Firebase";
                }
                
                AddActivityLog($"Added player to session: {playerName} with ${buyInAmount:F2} buy-in", ActivityType.PlayerAdded);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error adding player to session: {ex.Message}", "Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void UpdateNewSessionUI()
        {
            if (_sessionManager.CurrentSession != null)
            {
                // Update session title and ID
                SessionTitleText.Text = _sessionManager.CurrentSession.Name;
                SessionIdText.Text = $"ID: {_sessionManager.CurrentSessionId}";
                
                // Update total buy-ins
                var totalBuyIns = _sessionManager.Players.Sum(p => p.TotalBuyIn);
                StatusText.Text = $"Total Buy-ins: ${totalBuyIns:F2} | Players: {_sessionManager.Players.Count}";
                
                // Update empty state message visibility
                var hasPlayers = _sessionManager.Players.Count > 0;
                EmptyPlayersMessage.Visibility = hasPlayers ? Visibility.Collapsed : Visibility.Visible;
            }
        }
    }

    public class InputDialog : Window
    {
        public string Answer { get; private set; } = string.Empty;

        public InputDialog(string question, string title, string defaultAnswer = "")
        {
            Title = title;
            Width = 400;
            Height = 320;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.Transparent;
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            ShowInTaskbar = false;

            // Apply aero blur effect
            SourceInitialized += (s, e) => EnableAeroBlur();

            var mainBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(224, 0, 0, 0)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    Color = System.Windows.Media.Colors.Black,
                    Direction = 270,
                    ShadowDepth = 8,
                    Opacity = 0.5,
                    BlurRadius = 16
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title bar
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Title bar
            var titleBar = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 26, 26, 46)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 15, 52, 96)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(20, 15, 20, 15),
                Cursor = Cursors.Hand
            };
            
            // Add constrained drag functionality to title bar
            DialogConstraints.AddConstrainedDragHandler(titleBar, this);

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = title,
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);

            var closeButton = new Button
            {
                Content = "✕",
                Width = 32,
                Height = 32,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(closeButton, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(closeButton);
            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);

            // Content area
            var contentBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(160, 0, 0, 0)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(96, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(20, 10, 20, 10)
            };

            var contentStack = new StackPanel { Margin = new Thickness(15) };

            var questionLabel = new TextBlock
            {
                Text = question,
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 10)
            };

            var answerTextBox = new TextBox
            {
                Text = defaultAnswer,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                Padding = new Thickness(8, 6, 8, 6),
                VerticalAlignment = VerticalAlignment.Center
            };

            contentStack.Children.Add(questionLabel);
            contentStack.Children.Add(answerTextBox);
            contentBorder.Child = contentStack;
            Grid.SetRow(contentBorder, 1);

            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 52, 152, 219)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 41, 128, 185)),
                BorderThickness = new Thickness(1),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 52, 73, 94)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 44, 62, 80)),
                BorderThickness = new Thickness(1),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(titleBar);
            grid.Children.Add(contentBorder);
            grid.Children.Add(buttonPanel);

            mainBorder.Child = grid;
            Content = mainBorder;

            okButton.Click += (s, e) =>
            {
                Answer = answerTextBox.Text;
                DialogResult = true;
                Close();
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            answerTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    Answer = answerTextBox.Text;
                    DialogResult = true;
                    Close();
                }
                else if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };

            answerTextBox.Focus();
            answerTextBox.SelectAll();
        }

        private void EnableAeroBlur()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                
                // Enable blur behind window
                ACCENT_POLICY accentPolicy = new ACCENT_POLICY
                {
                    AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                    pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                    cbData = Marshal.SizeOf(accentPolicy)
                };

                Marshal.StructureToPtr(accentPolicy, data.pvData, false);

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.pvData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Aero blur not available: {ex.Message}");
            }
        }

        // Windows API structures for Aero blur
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
            WCA_ACCENT_POLICY = 19
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
            ACCENT_ENABLE_BLURBEHIND = 3
        }
    }

    public class FinalStackDialog : Window
    {
        public double Answer { get; private set; } = 0;

        public FinalStackDialog(string playerName, double currentBuyIn, double currentCashOut, double expectedStack)
        {
            Title = "Set Final Stack";
            Width = 450;
            Height = 380;
            WindowStartupLocation = WindowStartupLocation.CenterOwner;
            ResizeMode = ResizeMode.NoResize;
            Background = System.Windows.Media.Brushes.Transparent;
            AllowsTransparency = true;
            WindowStyle = WindowStyle.None;
            ShowInTaskbar = false;

            // Apply aero blur effect
            SourceInitialized += (s, e) => EnableAeroBlur();

            var mainBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(224, 0, 0, 0)),
                CornerRadius = new CornerRadius(8),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Effect = new DropShadowEffect
                {
                    Color = System.Windows.Media.Colors.Black,
                    Direction = 270,
                    ShadowDepth = 8,
                    Opacity = 0.5,
                    BlurRadius = 16
                }
            };

            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Title bar
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Content
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto }); // Buttons

            // Title bar
            var titleBar = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 26, 26, 46)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 15, 52, 96)),
                BorderThickness = new Thickness(0, 0, 0, 1),
                CornerRadius = new CornerRadius(8, 8, 0, 0),
                Padding = new Thickness(20, 15, 20, 15),
                Cursor = Cursors.Hand
            };
            
            // Add constrained drag functionality to title bar
            DialogConstraints.AddConstrainedDragHandler(titleBar, this);

            var titleGrid = new Grid();
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
            titleGrid.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });

            var titleText = new TextBlock
            {
                Text = "🏁 Set Final Stack",
                FontSize = 18,
                FontWeight = FontWeights.SemiBold,
                Foreground = System.Windows.Media.Brushes.White,
                VerticalAlignment = VerticalAlignment.Center
            };
            Grid.SetColumn(titleText, 0);

            var closeButton = new Button
            {
                Content = "✕",
                Width = 32,
                Height = 32,
                Background = System.Windows.Media.Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 16,
                Cursor = Cursors.Hand
            };
            closeButton.Click += (s, e) => { DialogResult = false; Close(); };
            Grid.SetColumn(closeButton, 1);

            titleGrid.Children.Add(titleText);
            titleGrid.Children.Add(closeButton);
            titleBar.Child = titleGrid;
            Grid.SetRow(titleBar, 0);

            // Content area
            var contentBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(160, 0, 0, 0)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(96, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(8),
                Margin = new Thickness(20, 10, 20, 10)
            };

            var scrollViewer = new ScrollViewer
            {
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                Background = System.Windows.Media.Brushes.Transparent
            };

            var contentStack = new StackPanel { Margin = new Thickness(15) };

            var instructionLabel = new TextBlock
            {
                Text = $"Enter current chip count for {playerName} (what they actually have on table):",
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };

            // Financial summary
            var summaryBorder = new Border
            {
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(80, 255, 255, 255)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                CornerRadius = new CornerRadius(4),
                Padding = new Thickness(10),
                Margin = new Thickness(0, 0, 0, 15)
            };

            var summaryStack = new StackPanel();
            summaryStack.Children.Add(new TextBlock { Text = $"Buy-in: ${currentBuyIn:F2}", Foreground = System.Windows.Media.Brushes.White, FontSize = 12 });
            summaryStack.Children.Add(new TextBlock { Text = $"Cash-outs: ${currentCashOut:F2}", Foreground = System.Windows.Media.Brushes.White, FontSize = 12 });
            summaryStack.Children.Add(new TextBlock { Text = $"Expected stack: ${expectedStack:F2}", Foreground = System.Windows.Media.Brushes.White, FontSize = 12 });
            summaryBorder.Child = summaryStack;

            var infoLabel = new TextBlock
            {
                Text = "This is just inventory - if less than expected, they lost chips to other players.",
                Foreground = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 255, 193, 7)),
                FontSize = 12,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, 0, 15)
            };

            var answerTextBox = new TextBox
            {
                Text = expectedStack.ToString("F2"),
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(64, 255, 255, 255)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(128, 255, 255, 255)),
                BorderThickness = new Thickness(1),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 14,
                Padding = new Thickness(8, 6, 8, 6),
                VerticalAlignment = VerticalAlignment.Center
            };

            contentStack.Children.Add(instructionLabel);
            contentStack.Children.Add(summaryBorder);
            contentStack.Children.Add(infoLabel);
            contentStack.Children.Add(answerTextBox);
            scrollViewer.Content = contentStack;
            contentBorder.Child = scrollViewer;
            Grid.SetRow(contentBorder, 1);

            // Button panel
            var buttonPanel = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                HorizontalAlignment = HorizontalAlignment.Right,
                Margin = new Thickness(20, 0, 20, 20)
            };

            var okButton = new Button
            {
                Content = "OK",
                Width = 80,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 92, 184, 92)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 76, 174, 76)),
                BorderThickness = new Thickness(1),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Margin = new Thickness(0, 0, 10, 0),
                Cursor = Cursors.Hand
            };

            var cancelButton = new Button
            {
                Content = "Cancel",
                Width = 80,
                Height = 32,
                Background = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 52, 73, 94)),
                BorderBrush = new System.Windows.Media.SolidColorBrush(System.Windows.Media.Color.FromArgb(255, 44, 62, 80)),
                BorderThickness = new Thickness(1),
                Foreground = System.Windows.Media.Brushes.White,
                FontSize = 12,
                FontWeight = FontWeights.SemiBold,
                Cursor = Cursors.Hand
            };

            buttonPanel.Children.Add(okButton);
            buttonPanel.Children.Add(cancelButton);
            Grid.SetRow(buttonPanel, 2);

            grid.Children.Add(titleBar);
            grid.Children.Add(contentBorder);
            grid.Children.Add(buttonPanel);

            mainBorder.Child = grid;
            Content = mainBorder;

            okButton.Click += (s, e) =>
            {
                if (double.TryParse(answerTextBox.Text, out double amount) && amount >= 0)
                {
                    Answer = amount;
                    DialogResult = true;
                    Close();
                }
                else
                {
                    MessageBox.Show("Please enter a valid amount (0 or greater).", "Invalid Amount", 
                        MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            };

            cancelButton.Click += (s, e) =>
            {
                DialogResult = false;
                Close();
            };

            answerTextBox.KeyDown += (s, e) =>
            {
                if (e.Key == Key.Enter)
                {
                    if (double.TryParse(answerTextBox.Text, out double amount) && amount >= 0)
                    {
                        Answer = amount;
                        DialogResult = true;
                        Close();
                    }
                    else
                    {
                        MessageBox.Show("Please enter a valid amount (0 or greater).", "Invalid Amount", 
                            MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else if (e.Key == Key.Escape)
                {
                    DialogResult = false;
                    Close();
                }
            };

            answerTextBox.Focus();
            answerTextBox.SelectAll();
        }

        private void EnableAeroBlur()
        {
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                
                // Enable blur behind window
                ACCENT_POLICY accentPolicy = new ACCENT_POLICY
                {
                    AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                    AccentFlags = 0,
                    GradientColor = 0,
                    AnimationId = 0
                };

                WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
                {
                    Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                    pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                    cbData = Marshal.SizeOf(accentPolicy)
                };

                Marshal.StructureToPtr(accentPolicy, data.pvData, false);

                SetWindowCompositionAttribute(hwnd, ref data);
                Marshal.FreeHGlobal(data.pvData);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Aero blur not available: {ex.Message}");
            }
        }

        // Windows API structures for Aero blur
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
            WCA_ACCENT_POLICY = 19
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
            ACCENT_ENABLE_BLURBEHIND = 3
        }
    }

    // Value converter for balance colors
    public class BalanceColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double balance)
            {
                if (Math.Abs(balance) < 0.01)
                    return System.Windows.Media.Colors.Gray;
                else if (balance > 0)
                    return System.Windows.Media.Colors.Green;
                else
                    return System.Windows.Media.Colors.Red;
            }
            return System.Windows.Media.Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProfitColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double profit)
            {
                if (Math.Abs(profit) < 0.01)
                    return System.Windows.Media.Colors.Gray;
                else if (profit > 0)
                    return System.Windows.Media.Colors.Green;
                else
                    return System.Windows.Media.Colors.Red;
            }
            return System.Windows.Media.Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class ProfitTextColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is double profit)
            {
                if (Math.Abs(profit) < 0.01)
                    return new SolidColorBrush(System.Windows.Media.Colors.White);  // Neutral - white
                else if (profit > 0)
                    return new SolidColorBrush(System.Windows.Media.Colors.LightGreen);  // Positive - green
                else
                    return new SolidColorBrush(System.Windows.Media.Colors.LightCoral);  // Negative - red
            }
            return new SolidColorBrush(System.Windows.Media.Colors.White);
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class StatusColorConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (value is bool isActive)
            {
                return isActive ? System.Windows.Media.Colors.Green : System.Windows.Media.Colors.Orange;
            }
            return System.Windows.Media.Colors.Gray;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    public class SessionVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            // Show the button only if there's a current session (not null)
            return value != null ? Visibility.Visible : Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}