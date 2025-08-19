using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using PokerTracker2.Models;
using PokerTracker2.Services;
using System.Threading.Tasks;

namespace PokerTracker2.Windows
{
    public partial class PlayerSelectionDialog : Window
    {
        private readonly PlayerManager _playerManager;
        private readonly SessionManager _sessionManager;
        private List<PlayerProfile> _availablePlayers;
        private PlayerProfile? _selectedPlayer;
        
        public PlayerProfile? SelectedPlayer => _selectedPlayer;
        public double BuyInAmount { get; private set; }
        public bool PlayerWasCreated { get; private set; }

        public PlayerSelectionDialog(SessionManager sessionManager)
        {
            InitializeComponent();
            _sessionManager = sessionManager;
            _playerManager = new PlayerManager();
            
            // Initialize PlayerManager and load players asynchronously
            _ = InitializeAsync();
            
            UpdateUI();
            
            // Add constrained drag handler to title bar
            DialogConstraints.AddConstrainedDragHandler(titleBar, this);
        }

        private async Task InitializeAsync()
        {
            try
            {
                // Initialize PlayerManager
                await _playerManager.InitializeAsync();
                
                // Load available players
                await LoadAvailablePlayersAsync();
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error initializing: {ex.Message}";
            }
        }

        private async Task LoadAvailablePlayersAsync()
        {
            try
            {
                // Get all players from PlayerManager (async)
                var allPlayers = await _playerManager.GetAllPlayersAsync();
                
                // Get current session players to exclude them
                var currentSessionPlayerNames = _sessionManager.GetPlayers()
                    .Select(p => p.Name.ToLower())
                    .ToHashSet();
                
                // Filter out players already in session and only show active players
                _availablePlayers = allPlayers
                    .Where(p => p.IsActive && !currentSessionPlayerNames.Contains(p.Name.ToLower()))
                    .OrderBy(p => p.Name)
                    .ToList();
                
                // Update the ListView
                PlayersListView.ItemsSource = _availablePlayers;
                
                // Update status message
                if (_availablePlayers.Count == 0)
                {
                    StatusMessage.Text = "No available players. All active players are already in session or create a new player.";
                }
                else
                {
                    StatusMessage.Text = $"{_availablePlayers.Count} available player(s). Select one or create new.";
                }
            }
            catch (Exception ex)
            {
                StatusMessage.Text = $"Error loading players: {ex.Message}";
            }
        }

        private void UpdateUI()
        {
            bool hasSelection = _selectedPlayer != null;
            AddPlayerButton.IsEnabled = hasSelection;
            
            if (hasSelection && _selectedPlayer != null)
            {
                // Show selected player preview
                SelectedPlayerPreview.Visibility = Visibility.Visible;
                SelectedPlayerName.Text = _selectedPlayer.DisplayName;
                SelectedPlayerStats.Text = $"Sessions: {_selectedPlayer.TotalSessionsPlayed} | " +
                                         $"Lifetime P/L: {_selectedPlayer.LifetimeProfit:C} | " +
                                         $"Created: {_selectedPlayer.CreatedDate:MM/dd/yyyy}";
                
                // Contact info
                var contactInfo = new List<string>();
                if (!string.IsNullOrWhiteSpace(_selectedPlayer.Email))
                    contactInfo.Add($"📧 {_selectedPlayer.Email}");
                if (!string.IsNullOrWhiteSpace(_selectedPlayer.Phone))
                    contactInfo.Add($"📞 {_selectedPlayer.Phone}");
                SelectedPlayerContact.Text = string.Join(" | ", contactInfo);
                
                // Notes
                SelectedPlayerNotes.Text = string.IsNullOrWhiteSpace(_selectedPlayer.Notes) 
                    ? "No notes" 
                    : $"📝 {_selectedPlayer.Notes}";
                    
                AddPlayerButton.Content = $"Add {_selectedPlayer.Name}";
            }
            else
            {
                SelectedPlayerPreview.Visibility = Visibility.Collapsed;
                AddPlayerButton.Content = "Add Player";
            }
        }

        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchTerm = SearchTextBox.Text?.ToLower() ?? "";
            
            // Handle placeholder visibility
            SearchPlaceholder.Visibility = string.IsNullOrEmpty(searchTerm) ? Visibility.Visible : Visibility.Collapsed;
            
            if (string.IsNullOrWhiteSpace(searchTerm))
            {
                PlayersListView.ItemsSource = _availablePlayers;
            }
            else
            {
                var filteredPlayers = _availablePlayers
                    .Where(p => p.SearchText.Contains(searchTerm) || 
                               p.Name.ToLower().Contains(searchTerm) ||
                               p.Nickname.ToLower().Contains(searchTerm))
                    .ToList();
                    
                PlayersListView.ItemsSource = filteredPlayers;
            }
        }

        private void PlayersListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            _selectedPlayer = PlayersListView.SelectedItem as PlayerProfile;
            UpdateUI();
        }

        private async void CreateNewPlayer_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Show the professional PlayerProfileDialog for creating new player
                var playerProfileDialog = new Dialogs.PlayerProfileDialog(null, null); // TODO: Pass PermissionService when available
                playerProfileDialog.Owner = this;
                
                if (playerProfileDialog.ShowDialog() == true)
                {
                    var playerProfile = playerProfileDialog.PlayerProfile;
                    
                    // Check if player already exists (PlayerProfileDialog should handle this, but double-check)
                    var existingPlayer = await _playerManager.GetPlayerAsync(playerProfile.Name);
                    if (existingPlayer != null)
                    {
                        MessageBox.Show($"Player '{playerProfile.Name}' already exists. Please select from the list instead.",
                            "Player Exists", MessageBoxButton.OK, MessageBoxImage.Warning);
                        return;
                    }

                    // Create the new player profile using PlayerManager
                    var success = await _playerManager.AddPlayerAsync(playerProfile);
                    if (!success)
                    {
                        MessageBox.Show($"Failed to create player '{playerProfile.Name}'. Please try again.",
                            "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                    
                    // Set as selected player
                    _selectedPlayer = playerProfile;
                    PlayerWasCreated = true;
                    
                    // Refresh the available players list to show the new player
                    await LoadAvailablePlayersAsync();
                    
                    // Ask for buy-in amount and close dialog
                    GetBuyInAmountAndClose();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error creating player: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void AddPlayer_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedPlayer == null)
            {
                MessageBox.Show("Please select a player first.", "No Selection", 
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            GetBuyInAmountAndClose();
        }

        private void GetBuyInAmountAndClose()
        {
            try
            {
                // Show professional input dialog for buy-in amount
                var inputDialog = new InputDialog($"Enter buy-in amount for {_selectedPlayer?.Name}:", "💰 Buy-in Amount", "0");
                inputDialog.Owner = this;
                
                if (inputDialog.ShowDialog() == true)
                {
                    var input = inputDialog.Answer;
                    if (double.TryParse(input, out double buyInAmount) && buyInAmount > 0)
                    {
                        BuyInAmount = buyInAmount;
                        DialogResult = true;
                    }
                    else if (!string.IsNullOrEmpty(input))
                    {
                        MessageBox.Show("Please enter a valid buy-in amount greater than 0.",
                            "Invalid Amount", MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    // If user cancels the input dialog (input is empty), just return without setting DialogResult
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error processing buy-in amount: {ex.Message}",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void CloseButton_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
        }
    }
}
