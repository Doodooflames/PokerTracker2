using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Text.Json;
using System.IO;
using System.Diagnostics;
using System.Windows;

namespace PokerTracker2.Services
{
    public class UpdateInfo
    {
        public string TagName { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Body { get; set; } = string.Empty;
        public DateTime PublishedAt { get; set; }
        public string HtmlUrl { get; set; } = string.Empty;
        public bool IsPrerelease { get; set; }
        public string DownloadUrl { get; set; } = string.Empty;
    }

    public class UpdateService
    {
        private readonly HttpClient _httpClient;
        private readonly string _currentVersion;
        private readonly string _githubApiUrl;
        private readonly string _githubRepoOwner;
        private readonly string _githubRepoName;

        public UpdateService()
        {
            _httpClient = new HttpClient();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "PokerTracker2-UpdateChecker");
            
            // Get current version from assembly
            _currentVersion = GetCurrentVersion();
            
            // GitHub configuration - these should be configurable
            _githubApiUrl = "https://api.github.com";
            _githubRepoOwner = "yourusername"; // TODO: Make configurable
            _githubRepoName = "PokerTracker2"; // TODO: Make configurable
        }

        public string CurrentVersion => _currentVersion;

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

        public async Task<UpdateInfo?> CheckForUpdatesAsync()
        {
            try
            {
                var url = $"{_githubApiUrl}/repos/{_githubRepoOwner}/{_githubRepoName}/releases/latest";
                var response = await _httpClient.GetStringAsync(url);
                
                var updateInfo = JsonSerializer.Deserialize<UpdateInfo>(response);
                
                if (updateInfo != null && IsNewerVersion(updateInfo.TagName))
                {
                    return updateInfo;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Log error but don't crash the app
                Debug.WriteLine($"Error checking for updates: {ex.Message}");
                return null;
            }
        }

        private bool IsNewerVersion(string newVersion)
        {
            try
            {
                // Remove 'v' prefix if present
                var cleanNewVersion = newVersion.TrimStart('v');
                var cleanCurrentVersion = _currentVersion.TrimStart('v');
                
                var newVersionParts = cleanNewVersion.Split('.');
                var currentVersionParts = cleanCurrentVersion.Split('.');
                
                // Compare version parts
                for (int i = 0; i < Math.Min(newVersionParts.Length, currentVersionParts.Length); i++)
                {
                    if (int.TryParse(newVersionParts[i], out int newPart) && 
                        int.TryParse(currentVersionParts[i], out int currentPart))
                    {
                        if (newPart > currentPart) return true;
                        if (newPart < currentPart) return false;
                    }
                }
                
                // If we get here, versions are equal up to the shorter length
                // Longer version is considered newer
                return newVersionParts.Length > currentVersionParts.Length;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> DownloadUpdateAsync(UpdateInfo updateInfo, string downloadPath)
        {
            try
            {
                if (string.IsNullOrEmpty(updateInfo.DownloadUrl))
                {
                    return false;
                }

                var response = await _httpClient.GetAsync(updateInfo.DownloadUrl);
                response.EnsureSuccessStatusCode();

                using (var fileStream = File.Create(downloadPath))
                {
                    await response.Content.CopyToAsync(fileStream);
                }

                return true;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error downloading update: {ex.Message}");
                return false;
            }
        }

        public void InstallUpdate(string updateFilePath)
        {
            try
            {
                // Create a batch file to handle the update process
                var batchContent = $@"
@echo off
timeout /t 2 /nobreak >nul
del ""{Process.GetCurrentProcess().MainModule?.FileName}""
copy ""{updateFilePath}"" ""{Process.GetCurrentProcess().MainModule?.FileName}""
start """" ""{Process.GetCurrentProcess().MainModule?.FileName}""
del ""{updateFilePath}""
del ""%~f0""
";

                var batchPath = Path.Combine(Path.GetTempPath(), "PokerTracker2_Update.bat");
                File.WriteAllText(batchPath, batchContent);

                // Start the batch file
                Process.Start(new ProcessStartInfo
                {
                    FileName = batchPath,
                    UseShellExecute = true,
                    CreateNoWindow = true
                });

                // Exit the current application
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error installing update: {ex.Message}");
                MessageBox.Show($"Error installing update: {ex.Message}", "Update Error", 
                    MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        public void OpenUpdatePage(UpdateInfo updateInfo)
        {
            try
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = updateInfo.HtmlUrl,
                    UseShellExecute = true
                });
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening update page: {ex.Message}");
            }
        }

        public void Dispose()
        {
            _httpClient?.Dispose();
        }
    }
}
