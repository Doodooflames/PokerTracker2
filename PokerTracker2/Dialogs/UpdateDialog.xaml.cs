using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using PokerTracker2.Services;

namespace PokerTracker2.Dialogs
{
    public partial class UpdateDialog : Window
    {
        // Windows API structures for Aero blur (copied from MainWindow)
        [DllImport("user32.dll")]
        private static extern int SetWindowCompositionAttribute(IntPtr hwnd, ref WINDOWCOMPOSITIONATTRIBDATA data);

        // Windows API for window regions (to contain blur)
        [DllImport("user32.dll")]
        private static extern int SetWindowRgn(IntPtr hWnd, IntPtr hRgn, bool bRedraw);

        [DllImport("gdi32.dll")]
        private static extern IntPtr CreateRoundRectRgn(int nLeftRect, int nTopRect, int nRightRect, int nBottomRect, int nWidthEllipse, int nHeightEllipse);

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

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
            ACCENT_ENABLE_BLURBEHIND = 3,
            ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
        }

        private readonly UpdateInfo _updateInfo;
        private readonly AutoUpdateService _updateService;

        public UpdateDialog(UpdateInfo updateInfo, AutoUpdateService updateService)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateService = updateService;
            
            // Enable Aero blur effect when window is initialized (same pattern as MainWindow)
            this.SourceInitialized += UpdateDialog_SourceInitialized;
            // Also apply region when loaded to ensure correct dimensions
            this.Loaded += UpdateDialog_Loaded;
            
            PopulateUpdateInfo();
        }

        private void PopulateUpdateInfo()
        {
            CurrentVersionText.Text = _updateInfo.CurrentVersion;
            LatestVersionText.Text = _updateInfo.LatestVersion;
            ReleaseDateText.Text = _updateInfo.PublishedAt?.ToString("MMMM dd, yyyy") ?? "Unknown";
            ReleaseNotesText.Text = _updateInfo.ReleaseNotes ?? "No release notes available.";
        }

        private async void UpdateButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show progress panel
                ProgressPanel.Visibility = Visibility.Visible;
                UpdateButton.IsEnabled = false;
                RemindLaterButton.IsEnabled = false;
                
                ProgressText.Text = "Starting update download...";
                
                // Start the update process
                var success = await _updateService.DownloadAndInstallUpdateAsync(_updateInfo);
                
                if (!success)
                {
                    MessageBox.Show("Update failed. Please try again later.", "Update Error", 
                        MessageBoxButton.OK, MessageBoxImage.Error);
                    
                    // Reset UI
                    ProgressPanel.Visibility = Visibility.Collapsed;
                    UpdateButton.IsEnabled = true;
                    RemindLaterButton.IsEnabled = true;
                }
                // If successful, the app will restart automatically
            }
            catch (Exception ex)
            {
                MessageBox.Show($"An error occurred during the update: {ex.Message}", "Update Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
                
                // Reset UI
                ProgressPanel.Visibility = Visibility.Collapsed;
                UpdateButton.IsEnabled = true;
                RemindLaterButton.IsEnabled = true;
            }
        }

        private void RemindLaterButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ButtonState == MouseButtonState.Pressed)
            {
                this.DragMove();
            }
        }

        private void UpdateDialog_SourceInitialized(object sender, EventArgs e)
        {
            EnableAeroBlur();
        }

        private void UpdateDialog_Loaded(object sender, RoutedEventArgs e)
        {
            // Reapply the region now that we have final dimensions
            try
            {
                IntPtr hwnd = new WindowInteropHelper(this).Handle;
                if (hwnd != IntPtr.Zero)
                {
                    ApplyWindowRegion(hwnd);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Region reapplication on load failed: {ex.Message}");
            }
        }

        private void EnableAeroBlur()
        {
            try
            {
                // Try Windows API blur with corrected parameters
                ApplyWindowsApiBlur();
                
                System.Diagnostics.Debug.WriteLine("Windows API blur attempted with corrected parameters");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Windows API blur error: {ex.Message}");
                
                // Fallback to WPF-only approach if Windows API fails
                try
                {
                    ApplyFallbackBlurEffect();
                }
                catch (Exception fallbackEx)
                {
                    System.Diagnostics.Debug.WriteLine($"Fallback blur error: {fallbackEx.Message}");
                }
            }
        }

        private void ApplyWindowsApiBlur()
        {
            IntPtr hwnd = new WindowInteropHelper(this).Handle;
            
            // First apply the window region to contain the blur
            ApplyWindowRegion(hwnd);
            
            // Then apply the blur effect
            ACCENT_POLICY accentPolicy = new ACCENT_POLICY
            {
                AccentState = ACCENT_STATE.ACCENT_ENABLE_BLURBEHIND,
                AccentFlags = 0, // Exactly like MainWindow
                GradientColor = 0, // Exactly like MainWindow
                AnimationId = 0
            };

            WINDOWCOMPOSITIONATTRIBDATA data = new WINDOWCOMPOSITIONATTRIBDATA
            {
                Attr = WINDOWCOMPOSITIONATTRIB.WCA_ACCENT_POLICY,
                pvData = Marshal.AllocHGlobal(Marshal.SizeOf(accentPolicy)),
                cbData = Marshal.SizeOf(accentPolicy)
            };

            Marshal.StructureToPtr(accentPolicy, data.pvData, false);

            int result = SetWindowCompositionAttribute(hwnd, ref data);
            Marshal.FreeHGlobal(data.pvData);
            
            System.Diagnostics.Debug.WriteLine($"Windows API blur result: {result}");
        }

        private void ApplyWindowRegion(IntPtr hwnd)
        {
            try
            {
                // Calculate the dialog dimensions for full-window region (outer padding now)
                double dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
                int width = (int)Math.Max(1, (this.ActualWidth > 0 ? this.ActualWidth : this.Width) * dpiScale);
                int height = (int)Math.Max(1, (this.ActualHeight > 0 ? this.ActualHeight : this.Height) * dpiScale);
                
                // No external margins anymore; use full window area
                int margin = 0;
                int contentWidth = width;
                int contentHeight = height;
                
                // Create a rounded rectangle region that matches our dialog content
                int cornerRadius = (int)(12 * dpiScale); // Match the CornerRadius="12" in XAML
                IntPtr region = CreateRoundRectRgn(margin, margin, margin + contentWidth, margin + contentHeight, cornerRadius, cornerRadius);
                
                if (region != IntPtr.Zero)
                {
                    int regionResult = SetWindowRgn(hwnd, region, true);
                    System.Diagnostics.Debug.WriteLine($"Window region applied: {regionResult}, Dimensions: {contentWidth}x{contentHeight}, Corner radius: {cornerRadius}");
                    
                    // Note: Don't delete the region object here, Windows takes ownership
                }
                else
                {
                    System.Diagnostics.Debug.WriteLine("Failed to create window region");
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Window region application failed: {ex.Message}");
            }
        }

        private void ApplyFallbackBlurEffect()
        {
            // Simple fallback - just ensure transparency is maintained
            System.Diagnostics.Debug.WriteLine("Using WPF-only transparency fallback");
        }
    }
}
