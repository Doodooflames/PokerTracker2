using System;
using System.Windows;
using PokerTracker2.Windows;

namespace PokerTracker2
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            try
            {
                Console.WriteLine("=== APPLICATION STARTUP START ===");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] OnStartup called");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Calling base.OnStartup...");
                
                base.OnStartup(e);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] base.OnStartup completed successfully");
                
                // Create and show the login window first
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Creating LoginWindow...");
                LoginWindow loginWindow = new LoginWindow();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LoginWindow created successfully");
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Showing LoginWindow...");
                loginWindow.Show();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] LoginWindow.Show() completed successfully");
                
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Application startup completed successfully");
                Console.WriteLine("=== APPLICATION STARTUP COMPLETE ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] CRITICAL ERROR in OnStartup: {ex.Message}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Exception type: {ex.GetType().Name}");
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Stack trace: {ex.StackTrace}");
                
                // Try to show error in message box if possible
                try
                {
                    MessageBox.Show($"Critical error during application startup:\n\n{ex.Message}\n\n{ex.StackTrace}", 
                                  "Application Startup Error", 
                                  MessageBoxButton.OK, 
                                  MessageBoxImage.Error);
                }
                catch
                {
                    // If even MessageBox fails, just continue with console output
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Could not show error message box");
                }
                
                // Re-throw to prevent silent failure
                throw;
            }
        }
        
        protected override void OnExit(ExitEventArgs e)
        {
            try
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Application exiting with code: {e.ApplicationExitCode}");
                base.OnExit(e);
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] Application exit completed");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss.fff}] ERROR in OnExit: {ex.Message}");
            }
        }
    }
} 