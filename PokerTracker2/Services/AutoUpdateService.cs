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

                // Download the update
                var updateBytes = await _httpClient.GetByteArrayAsync(updateInfo.DownloadUrl);
                var updatePath = Path.Combine(Path.GetTempPath(), $"PokerTracker2-{updateInfo.LatestVersion}.exe");
                
                await File.WriteAllBytesAsync(updatePath, updateBytes);
                LoggingService.Instance.Info($"Update downloaded to: {updatePath}", "AutoUpdateService");

                // Show success message and instructions
                var result = MessageBox.Show(
                    $"Update downloaded successfully to:\n{updatePath}\n\n" +
                    "To install the update:\n" +
                    "1. Close this application\n" +
                    "2. Run the downloaded file\n" +
                    "3. Follow the installation instructions\n\n" +
                    "Would you like to open the download folder now?",
                    "Update Downloaded",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Information);

                if (result == MessageBoxResult.Yes)
                {
                    Process.Start("explorer.exe", $"/select,\"{updatePath}\"");
                }

                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error downloading update: {ex.Message}", "AutoUpdateService", ex);
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
