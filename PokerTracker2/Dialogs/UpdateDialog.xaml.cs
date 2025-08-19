using System;
using System.Windows;
using PokerTracker2.Services;

namespace PokerTracker2.Dialogs
{
    public partial class UpdateDialog : Window
    {
        private readonly UpdateInfo _updateInfo;
        private readonly AutoUpdateService _updateService;

        public UpdateDialog(UpdateInfo updateInfo, AutoUpdateService updateService)
        {
            InitializeComponent();
            _updateInfo = updateInfo;
            _updateService = updateService;
            
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
    }
}
