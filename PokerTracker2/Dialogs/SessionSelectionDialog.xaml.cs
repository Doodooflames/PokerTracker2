using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using System.Windows.Controls;
using PokerTracker2.Models;
using PokerTracker2.Services;

namespace PokerTracker2.Dialogs
{
    public partial class SessionSelectionDialog : Window
    {
        public Session? SelectedSession { get; private set; }

        public SessionSelectionDialog(List<Session> activeSessions, List<Session> completedSessions)
        {
            try
            {
                InitializeComponent();
                
                // Validate input parameters
                if (activeSessions == null)
                {
                    LoggingService.Instance.Warning("ActiveSessions is null, using empty list", "SessionSelectionDialog");
                    activeSessions = new List<Session>();
                }
                
                if (completedSessions == null)
                {
                    LoggingService.Instance.Warning("CompletedSessions is null, using empty list", "SessionSelectionDialog");
                    completedSessions = new List<Session>();
                }
                
                // Set up data sources
                ActiveSessionsList.ItemsSource = activeSessions;
                CompletedSessionsList.ItemsSource = completedSessions;
                
                // Show no sessions message if no sessions exist
                if (activeSessions.Count == 0 && completedSessions.Count == 0)
                {
                    NoSessionsText.Visibility = Visibility.Visible;
                }
                
                // Set owner to center on parent window (with null check to prevent crash)
                try
                {
                    this.Owner = Application.Current.MainWindow;
                }
                catch
                {
                    // If MainWindow is not available, continue without owner
                    LoggingService.Instance.Warning("Could not set dialog owner - MainWindow not available", "SessionSelectionDialog");
                }
                
                // Add constrained drag handler to title bar
                try
                {
                    DialogConstraints.AddConstrainedDragHandler(titleBar, this);
                }
                catch (Exception ex)
                {
                    LoggingService.Instance.Warning($"Could not add drag handler: {ex.Message}", "SessionSelectionDialog");
                    // Continue without drag functionality
                }
                
                LoggingService.Instance.Debug($"SessionSelectionDialog initialized with {activeSessions.Count} active and {completedSessions.Count} completed sessions", "SessionSelectionDialog");
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to initialize SessionSelectionDialog: {ex.Message}", "SessionSelectionDialog", ex);
                throw; // Re-throw to let caller handle the error
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }

        private void LoadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SelectedSession != null)
            {
                this.DialogResult = true;
                this.Close();
            }
        }

        private void SessionItem_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (sender is FrameworkElement element && element.Tag is Session session)
            {
                SelectedSession = session;
                LoadButton.IsEnabled = true;
                
                // Update selected session info
                SelectedSessionInfo.Text = $"Selected: {session.Name} ({session.StatusText})";
                
                // Visual feedback - highlight selected item
                ClearSelection();
                if (element is Border border)
                {
                    border.Background = System.Windows.Media.Brushes.DarkBlue;
                    border.BorderBrush = System.Windows.Media.Brushes.White;
                }
            }
        }

        private void ClearSelection()
        {
            // Clear selection from active sessions
            foreach (var item in ActiveSessionsList.Items)
            {
                var container = ActiveSessionsList.ItemContainerGenerator.ContainerFromItem(item);
                if (container is FrameworkElement element)
                {
                    var border = FindBorder(element);
                    if (border != null)
                    {
                        border.Background = System.Windows.Media.Brushes.Transparent;
                        border.BorderBrush = System.Windows.Media.Brushes.Gray;
                    }
                }
            }
            
            // Clear selection from completed sessions
            foreach (var item in CompletedSessionsList.Items)
            {
                var container = CompletedSessionsList.ItemContainerGenerator.ContainerFromItem(item);
                if (container is FrameworkElement element)
                {
                    var border = FindBorder(element);
                    if (border != null)
                    {
                        border.Background = System.Windows.Media.Brushes.Transparent;
                        border.BorderBrush = System.Windows.Media.Brushes.Gray;
                    }
                }
            }
        }

        private Border? FindBorder(FrameworkElement element)
        {
            if (element is Border border)
                return border;
            
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(element); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(element, i) as FrameworkElement;
                if (child != null)
                {
                    var result = FindBorder(child);
                    if (result != null)
                        return result;
                }
            }
            
            return null;
        }
    }
}
