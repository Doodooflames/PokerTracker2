using System;
using System.Reflection;

namespace PokerTracker2.Services
{
    public static class VersionService
    {
        private static string? _cachedVersion;
        private static string? _cachedFileVersion;
        private static string? _cachedAssemblyVersion;

        /// <summary>
        /// Gets the current application version from the assembly
        /// </summary>
        public static string CurrentVersion
        {
            get
            {
                if (_cachedVersion == null)
                {
                    try
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        // Use AssemblyVersion instead of AssemblyInformationalVersion to avoid Git commit hash
                        var version = assembly.GetName().Version;
                        if (version != null)
                        {
                            // Return the full version string (e.g., "1.0.7.0")
                            _cachedVersion = version.ToString();
                        }
                        else
                        {
                            _cachedVersion = "1.0.15.0";
                        }
                    }
                    catch
                    {
                        _cachedVersion = "1.0.15.0";
                    }
                }
                return _cachedVersion;
            }
        }

        /// <summary>
        /// Gets the current file version from the assembly
        /// </summary>
        public static string CurrentFileVersion
        {
            get
            {
                if (_cachedFileVersion == null)
                {
                    try
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var fileVersionAttribute = assembly.GetCustomAttribute<AssemblyFileVersionAttribute>();
                        
                        if (fileVersionAttribute != null && !string.IsNullOrEmpty(fileVersionAttribute.Version))
                        {
                            _cachedFileVersion = fileVersionAttribute.Version;
                        }
                        else
                        {
                            // Fallback to assembly version
                            var version = assembly.GetName().Version;
                            _cachedFileVersion = version?.ToString() ?? "1.0.15.0";
                        }
                    }
                    catch
                    {
                        _cachedFileVersion = "1.0.15.0";
                    }
                }
                return _cachedFileVersion;
            }
        }

        /// <summary>
        /// Gets the current assembly version
        /// </summary>
        public static string CurrentAssemblyVersion
        {
            get
            {
                if (_cachedAssemblyVersion == null)
                {
                    try
                    {
                        var assembly = Assembly.GetExecutingAssembly();
                        var version = assembly.GetName().Version;
                        _cachedAssemblyVersion = version?.ToString() ?? "1.0.15.0";
                    }
                    catch
                    {
                        _cachedAssemblyVersion = "1.0.15.0";
                    }
                }
                return _cachedAssemblyVersion;
            }
        }

        /// <summary>
        /// Gets the formatted version string for display (e.g., "v1.0.4")
        /// </summary>
        public static string FormattedVersion => $"v{CurrentVersion}";

        /// <summary>
        /// Gets the full version string for display (e.g., "v1.0.4.0")
        /// </summary>
        public static string FullFormattedVersion => $"v{CurrentFileVersion}";

        /// <summary>
        /// Clears the cached version information (useful for testing)
        /// </summary>
        public static void ClearCache()
        {
            _cachedVersion = null;
            _cachedFileVersion = null;
            _cachedAssemblyVersion = null;
        }
    }
}
