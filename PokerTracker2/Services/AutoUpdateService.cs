using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Windows;
using Newtonsoft.Json;
using PokerTracker2.Services;

namespace PokerTracker2.Services
{
    public class AutoUpdateService
    {
        private readonly string _updateUrl;
        private readonly string _appName;
        private readonly string _currentVersion;
        private readonly HttpClient _httpClient;

        public AutoUpdateService()
        {
            _updateUrl = "https://api.github.com/repos/Doodooflames/PokerTracker2/releases/latest";
            _appName = "PokerTracker2";
            _currentVersion = GetCurrentVersion();
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PokerTracker2-UpdateChecker");
        }

        public async Task<UpdateInfo> CheckForUpdatesAsync()
        {
            try
            {
                LoggingService.Instance.Info("Checking for updates...", "AutoUpdateService");
                
                var response = await _httpClient.GetStringAsync(_updateUrl);
                var release = JsonConvert.DeserializeObject<GitHubRelease>(response);
                
                if (release == null)
                {
                    LoggingService.Instance.Warning("Failed to parse GitHub release response", "AutoUpdateService");
                    return null;
                }

                var latestVersion = new Version(release.TagName.TrimStart('v'));
                var currentVersion = new Version(_currentVersion);

                LoggingService.Instance.Info($"Current version: {currentVersion}, Latest version: {latestVersion}", "AutoUpdateService");

                if (latestVersion > currentVersion)
                {
                    var updateInfo = new UpdateInfo
                    {
                        IsUpdateAvailable = true,
                        LatestVersion = latestVersion.ToString(),
                        CurrentVersion = currentVersion.ToString(),
                        ReleaseNotes = release.Body,
                        DownloadUrl = release.Assets?.FirstOrDefault(a => a.Name.EndsWith(".exe"))?.BrowserDownloadUrl,
                        PublishedAt = release.PublishedAt
                    };

                    LoggingService.Instance.Info($"Update available: {updateInfo.LatestVersion}", "AutoUpdateService");
                    return updateInfo;
                }

                LoggingService.Instance.Info("No updates available", "AutoUpdateService");
                return new UpdateInfo { IsUpdateAvailable = false };
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error checking for updates: {ex.Message}", "AutoUpdateService", ex);
                return null;
            }
        }

        public async Task<bool> DownloadAndInstallUpdateAsync(UpdateInfo updateInfo)
        {
            try
            {
                if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    LoggingService.Instance.Error("No download URL available for update", "AutoUpdateService");
                    return false;
                }

                LoggingService.Instance.Info($"Starting update download: {updateInfo.LatestVersion}", "AutoUpdateService");

                // Get current executable path and directory
                var currentExePath = Process.GetCurrentProcess().MainModule?.FileName;
                if (string.IsNullOrEmpty(currentExePath))
                {
                    LoggingService.Instance.Error("Could not determine current executable path", "AutoUpdateService");
                    return false;
                }

                var currentExeDir = Path.GetDirectoryName(currentExePath);
                var currentExeName = Path.GetFileName(currentExePath);
                var backupExePath = Path.Combine(currentExeDir, $"{Path.GetFileNameWithoutExtension(currentExeName)}_old.exe");
                var newExePath = Path.Combine(currentExeDir, $"{Path.GetFileNameWithoutExtension(currentExeName)}_new.exe");

                LoggingService.Instance.Info($"Current exe: {currentExePath}", "AutoUpdateService");
                LoggingService.Instance.Info($"New exe will be: {newExePath}", "AutoUpdateService");

                // Download the update to the same directory as current exe
                var updateBytes = await _httpClient.GetByteArrayAsync(updateInfo.DownloadUrl);
                await File.WriteAllBytesAsync(newExePath, updateBytes);
                
                LoggingService.Instance.Info($"Update downloaded to: {newExePath}", "AutoUpdateService");

                // Confirm download was successful
                if (!File.Exists(newExePath))
                {
                    LoggingService.Instance.Error("Downloaded file does not exist", "AutoUpdateService");
                    return false;
                }

                // Show update ready message
                var result = MessageBox.Show(
                    $"Update {updateInfo.LatestVersion} downloaded successfully!\n\n" +
                    "The application will now close and restart with the new version.\n" +
                    "The old version will be automatically cleaned up.\n\n" +
                    "Continue with update?",
                    "Update Ready",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (result != MessageBoxResult.Yes)
                {
                    // Clean up downloaded file if user cancels
                    File.Delete(newExePath);
                    LoggingService.Instance.Info("Update cancelled by user", "AutoUpdateService");
                    return false;
                }

                LoggingService.Instance.Info("Starting update process...", "AutoUpdateService");

                // Create batch script to handle the update process
                var batchScript = Path.Combine(Path.GetTempPath(), "PokerTracker2_Update.bat");
                var batchContent = $@"@echo off
echo Starting PokerTracker2 update process...

REM Wait for current process to close
timeout /t 2 /nobreak >nul

REM Backup current executable
if exist ""{currentExePath}"" (
    echo Backing up current version...
    move ""{currentExePath}"" ""{backupExePath}""
)

REM Move new executable to replace old one
if exist ""{newExePath}"" (
    echo Installing new version...
    move ""{newExePath}"" ""{currentExePath}""
)

REM Start new version
echo Starting new version...
start """" ""{currentExePath}""

REM Wait a moment for new process to start
timeout /t 3 /nobreak >nul

REM Check if new process is running, then clean up old version
tasklist /FI ""IMAGENAME eq {currentExeName}"" 2>nul | find /I ""{currentExeName}"" >nul
if %ERRORLEVEL% EQU 0 (
    echo New version started successfully, cleaning up...
    if exist ""{backupExePath}"" (
        del ""{backupExePath}""
    )
    echo Update completed successfully!
) else (
    echo New version failed to start, restoring backup...
    if exist ""{backupExePath}"" (
        move ""{backupExePath}"" ""{currentExePath}""
    )
    echo Update failed, original version restored.
)

REM Clean up this script
del ""%~f0""";

                await File.WriteAllTextAsync(batchScript, batchContent);
                LoggingService.Instance.Info($"Update script created: {batchScript}", "AutoUpdateService");

                // Start the update batch script
                var processInfo = new ProcessStartInfo
                {
                    FileName = batchScript,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };

                Process.Start(processInfo);
                LoggingService.Instance.Info("Update script started, closing application...", "AutoUpdateService");

                // Close the current application
                Application.Current.Dispatcher.Invoke(() =>
                {
                    Application.Current.Shutdown();
                });

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error downloading and installing update: {ex.Message}", "AutoUpdateService", ex);
                return false;
            }
        }

        private string GetCurrentVersion()
        {
            try
            {
                var assembly = System.Reflection.Assembly.GetExecutingAssembly();
                var version = assembly.GetName().Version;
                return version?.ToString() ?? "1.0.0.0";
            }
            catch
            {
                return "1.0.0.0";
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }

    public class UpdateInfo
    {
        public bool IsUpdateAvailable { get; set; }
        public string LatestVersion { get; set; }
        public string CurrentVersion { get; set; }
        public string ReleaseNotes { get; set; }
        public string DownloadUrl { get; set; }
        public DateTime? PublishedAt { get; set; }
    }

    public class GitHubRelease
    {
        [JsonProperty("tag_name")]
        public string TagName { get; set; }

        [JsonProperty("body")]
        public string Body { get; set; }

        [JsonProperty("published_at")]
        public DateTime? PublishedAt { get; set; }

        [JsonProperty("assets")]
        public GitHubAsset[] Assets { get; set; }
    }

    public class GitHubAsset
    {
        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("browser_download_url")]
        public string BrowserDownloadUrl { get; set; }
    }
}
