using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using System.IO;

namespace PokerTracker2.Services
{
    /// <summary>
    /// Unified logging service that centralizes all application logging through the debug console
    /// with fallback file logging to prevent data loss on crashes
    /// </summary>
    public class LoggingService : INotifyPropertyChanged
    {
        private static LoggingService? _instance;
        private static readonly object _lock = new object();
        
        // Debug callback for real-time logging to UI
        public static Action<string>? DebugCallback { get; set; }
        
        // Log levels
        public enum LogLevel
        {
            Debug = 0,
            Info = 1,
            Warning = 2,
            Error = 3,
            Critical = 4
        }
        
        // Current log level setting
        private LogLevel _currentLogLevel = LogLevel.Info;
        
        // Log buffer for startup messages before debug console is ready
        private readonly Queue<string> _startupLogBuffer = new Queue<string>();
        private bool _debugConsoleReady = false;
        
        // File logging for crash protection
        private readonly string _logFilePath;
        private readonly object _fileLock = new object();
        
        public static LoggingService Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        _instance ??= new LoggingService();
                    }
                }
                return _instance;
            }
        }
        
        private LoggingService()
        {
            // Set up log file path in project root
            var projectRoot = Directory.GetCurrentDirectory();
            if (projectRoot.EndsWith("PokerTracker2"))
            {
                // We're in the project subdirectory, go up one level
                projectRoot = Directory.GetParent(projectRoot)?.FullName ?? projectRoot;
            }
            _logFilePath = Path.Combine(projectRoot, "lastlog.txt");
            
            // Clear the log file on each app launch
            try
            {
                lock (_fileLock)
                {
                    File.WriteAllText(_logFilePath, $"=== PokerTracker2 Log Started: {DateTime.Now:yyyy-MM-dd HH:mm:ss} ==={Environment.NewLine}");
                }
            }
            catch
            {
                // If we can't write to the log file, continue without file logging
            }
            
            // Buffer startup messages until debug console is ready
            Log(LogLevel.Info, "LoggingService initialized - buffering messages until debug console is ready");
        }
        
        /// <summary>
        /// Set the debug console as ready to receive log messages
        /// </summary>
        public void SetDebugConsoleReady()
        {
            _debugConsoleReady = true;
            
            // Flush any buffered startup messages
            while (_startupLogBuffer.Count > 0)
            {
                var message = _startupLogBuffer.Dequeue();
                SendToDebugConsole(message);
            }
            
            Log(LogLevel.Info, "Debug console is now ready - all logging will appear in real-time");
        }
        
        /// <summary>
        /// Set the current log level
        /// </summary>
        public void SetLogLevel(LogLevel level)
        {
            _currentLogLevel = level;
            Log(LogLevel.Info, $"Log level set to: {level}");
        }
        
        /// <summary>
        /// Get the current log level
        /// </summary>
        public LogLevel CurrentLogLevel => _currentLogLevel;
        
        /// <summary>
        /// Main logging method - logs messages based on current log level
        /// </summary>
        public void Log(LogLevel level, string message, string? source = null, Exception? exception = null)
        {
            // Check if message should be logged based on current level
            if (level < _currentLogLevel)
                return;
            
            // Build the log message
            var timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
            var levelText = GetLevelText(level);
            var sourceText = !string.IsNullOrEmpty(source) ? $"[{source}] " : "";
            var exceptionText = exception != null ? $"\nException: {exception.GetType().Name}: {exception.Message}\nStackTrace: {exception.StackTrace}" : "";
            
            var fullMessage = $"[{timestamp}] {levelText} {sourceText}{message}{exceptionText}";
            
            // Always write to file for crash protection
            WriteToLogFile(fullMessage);
            
            // Send to debug console if ready, otherwise buffer
            if (_debugConsoleReady)
            {
                SendToDebugConsole(fullMessage);
            }
            else
            {
                // Buffer startup messages
                if (_startupLogBuffer.Count < 100) // Prevent memory issues
                {
                    _startupLogBuffer.Enqueue(fullMessage);
                }
            }
        }
        
        /// <summary>
        /// Write message to log file for crash protection
        /// </summary>
        private void WriteToLogFile(string message)
        {
            try
            {
                lock (_fileLock)
                {
                    File.AppendAllText(_logFilePath, message + Environment.NewLine);
                }
            }
            catch
            {
                // If file logging fails, we can't log that failure, so just ignore
                // This ensures the app doesn't crash due to logging issues
            }
        }
        
        /// <summary>
        /// Log debug message
        /// </summary>
        public void Debug(string message, string? source = null)
        {
            Log(LogLevel.Debug, message, source);
        }
        
        /// <summary>
        /// Log info message
        /// </summary>
        public void Info(string message, string? source = null)
        {
            Log(LogLevel.Info, message, source);
        }
        
        /// <summary>
        /// Log warning message
        /// </summary>
        public void Warning(string message, string? source = null)
        {
            Log(LogLevel.Warning, message, source);
        }
        
        /// <summary>
        /// Log error message
        /// </summary>
        public void Error(string message, string? source = null, Exception? exception = null)
        {
            Log(LogLevel.Error, message, source, exception);
        }
        
        /// <summary>
        /// Log critical message
        /// </summary>
        public void Critical(string message, string? source = null, Exception? exception = null)
        {
            Log(LogLevel.Critical, message, source, exception);
        }
        
        /// <summary>
        /// Send message to debug console via callback
        /// </summary>
        private void SendToDebugConsole(string message)
        {
            try
            {
                DebugCallback?.Invoke(message);
            }
            catch
            {
                // If debug console fails, we can't log that failure, so just ignore
            }
        }
        
        /// <summary>
        /// Get human-readable log level text
        /// </summary>
        private string GetLevelText(LogLevel level)
        {
            return level switch
            {
                LogLevel.Debug => "🐛",
                LogLevel.Info => "ℹ️",
                LogLevel.Warning => "⚠️",
                LogLevel.Error => "❌",
                LogLevel.Critical => "🚨",
                _ => "❓"
            };
        }
        
        /// <summary>
        /// Clear the startup log buffer
        /// </summary>
        public void ClearStartupBuffer()
        {
            _startupLogBuffer.Clear();
        }
        
        /// <summary>
        /// Get the number of buffered startup messages
        /// </summary>
        public int BufferedMessageCount => _startupLogBuffer.Count;
        
        /// <summary>
        /// Get the path to the log file
        /// </summary>
        public string LogFilePath => _logFilePath;
        
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
    }
}
