using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.Threading.Tasks;
using System.IO;
using PokerTracker2.Models;
using PokerTracker2.Services;

namespace PokerTracker2.Services
{
    public class PlayerManager : INotifyPropertyChanged
    {
        private readonly ObservableCollection<PlayerProfile> _players;
        private readonly FirebaseService _firebaseService;
        private bool _isInitialized = false;

        public ObservableCollection<PlayerProfile> Players => _players;



        public PlayerManager()
        {
            _players = new ObservableCollection<PlayerProfile>();
            _firebaseService = new FirebaseService();
            LoggingService.Instance.Info("PlayerManager constructor completed", "PlayerManager");
        }

        /// <summary>
        /// Initialize the PlayerManager by loading login profiles from Firebase
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_isInitialized) return true;

                LoggingService.Instance.Info("Initializing PlayerManager...", "PlayerManager");
                
                // Load only profiles with passwords for login (minimal startup data)
                var loginProfiles = await _firebaseService.GetLoginProfilesAsync();
                
                _players.Clear();
                foreach (var profile in loginProfiles)
                {
                    _players.Add(profile);
                }

                _isInitialized = true;
                LoggingService.Instance.Info($"PlayerManager initialized with {_players.Count} login profiles", "PlayerManager");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Failed to initialize PlayerManager: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Add a new player profile and save to Firebase immediately
        /// </summary>
        public async Task<bool> AddPlayerAsync(PlayerProfile player)
        {
            try
            {
                LoggingService.Instance.Info($"Adding player: {player.Name}", "PlayerManager");
                LoggingService.Instance.Info($"Player details: Nickname='{player.Nickname}', Email='{player.Email}', HasPassword={player.HasPassword}", "PlayerManager");
                
                // Save to Firebase immediately
                LoggingService.Instance.Info($"Attempting to save player {player.Name} to Firebase...", "PlayerManager");
                var success = await _firebaseService.SavePlayerProfileAsync(player);
                if (!success)
                {
                    LoggingService.Instance.Critical($"Failed to save player {player.Name} to Firebase", "PlayerManager");
                    LoggingService.Instance.Warning($"This indicates a Firebase connection or permission issue", "PlayerManager");
                    LoggingService.Instance.Warning($"Check the debug output for FirebaseService error details", "PlayerManager");
                    return false;
                }

                LoggingService.Instance.Info($"Successfully saved player {player.Name} to Firebase", "PlayerManager");

                // Add to local collection if it has a password (for login)
                if (player.HasPassword)
                {
                    _players.Add(player);
                    LoggingService.Instance.Info($"Added player {player.Name} to local login collection", "PlayerManager");
                }
                else
                {
                    LoggingService.Instance.Info($"Player {player.Name} has no password, not adding to login collection", "PlayerManager");
                }

                LoggingService.Instance.Info($"Successfully added player: {player.Name}", "PlayerManager");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Critical($"Exception adding player {player.Name}: {ex.GetType().Name}: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Add a new player by name and basic info (backward compatibility)
        /// </summary>
        public async Task<PlayerProfile> AddPlayer(string name, string nickname = "", string email = "", string phone = "", string notes = "")
        {
            try
            {
                var player = new PlayerProfile(name, nickname)
                {
                    Email = email,
                    Phone = phone,
                    Notes = notes
                };

                var success = await AddPlayerAsync(player);
                if (!success)
                {
                    var errorDetails = $"Failed to add player '{name}'. This usually means:\n" +
                                     "• Firebase connection failed\n" +
                                     "• Player profile couldn't be saved to Firebase\n" +
                                     "• Check the debug output for detailed error information\n\n" +
                                     "Player details:\n" +
                                     $"• Name: {name}\n" +
                                     $"• Nickname: {nickname}\n" +
                                     $"• Email: {email}\n" +
                                     $"• Has Password: {player.HasPassword}\n" +
                                     $"• Created Date: {player.CreatedDate}\n\n" +
                                     "Please try again or check the Firebase connection.";
                    
                    throw new InvalidOperationException(errorDetails);
                }

                return player;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception in AddPlayer for {name}: {ex.Message}", "PlayerManager", ex);
                throw;
            }
        }

        /// <summary>
        /// Update an existing player profile and save to Firebase immediately
        /// </summary>
        public async Task<bool> UpdatePlayerAsync(PlayerProfile player)
        {
            try
            {
                LoggingService.Instance.Info($"Updating player: {player.Name}", "PlayerManager");
                
                // Save to Firebase immediately
                var success = await _firebaseService.SavePlayerProfileAsync(player);
                if (!success)
                {
                    LoggingService.Instance.Error($"Failed to update player {player.Name} in Firebase", "PlayerManager");
                    return false;
                }

                // Update local collection if it has a password
                if (player.HasPassword)
                {
                    var existingPlayer = _players.FirstOrDefault(p => p.Name == player.Name);
                    if (existingPlayer != null)
                    {
                        // Update properties
                        existingPlayer.Nickname = player.Nickname;
                        existingPlayer.Email = player.Email;
                        existingPlayer.Phone = player.Phone;
                        existingPlayer.Notes = player.Notes;
                        existingPlayer.HasPassword = player.HasPassword;
                        existingPlayer.PasswordHash = player.PasswordHash;
                        existingPlayer.Salt = player.Salt;
                        existingPlayer.CreatedDate = player.CreatedDate;
                        existingPlayer.LastPlayedDate = player.LastPlayedDate;
                        existingPlayer.TotalSessionsPlayed = player.TotalSessionsPlayed;
                        existingPlayer.TotalLifetimeBuyIn = player.TotalLifetimeBuyIn;
                        existingPlayer.TotalLifetimeCashOut = player.TotalLifetimeCashOut;
                        existingPlayer.IsActive = player.IsActive;
                    }
                    else
                    {
                        // Player wasn't in login collection but now has password
                        _players.Add(player);
                    }
                }
                else
                {
                    // Player no longer has password, remove from login collection
                    var existingPlayer = _players.FirstOrDefault(p => p.Name == player.Name);
                    if (existingPlayer != null)
                    {
                        _players.Remove(existingPlayer);
                    }
                }

                LoggingService.Instance.Info($"Successfully updated player: {player.Name}", "PlayerManager");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception updating player {player.Name}: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Update player (backward compatibility)
        /// </summary>
        public async Task UpdatePlayer(PlayerProfile player)
        {
            var success = await UpdatePlayerAsync(player);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to update player {player.Name}");
            }
        }

        /// <summary>
        /// Delete a player profile from Firebase and local collection
        /// </summary>
        public async Task<bool> DeletePlayerAsync(string playerName)
        {
            try
            {
                LoggingService.Instance.Info($"Deleting player: {playerName}", "PlayerManager");
                
                // Delete from Firebase
                var success = await _firebaseService.DeletePlayerProfileAsync(playerName);
                if (!success)
                {
                    LoggingService.Instance.Error($"Failed to delete player {playerName} from Firebase", "PlayerManager");
                    return false;
                }

                // Remove from local collection
                var player = _players.FirstOrDefault(p => p.Name == playerName);
                if (player != null)
                {
                    _players.Remove(player);
                }

                LoggingService.Instance.Info($"Successfully deleted player: {playerName}", "PlayerManager");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception deleting player {playerName}: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Delete player (backward compatibility)
        /// </summary>
        public async Task DeletePlayer(string playerName)
        {
            var success = await DeletePlayerAsync(playerName);
            if (!success)
            {
                throw new InvalidOperationException($"Failed to delete player {playerName}");
            }
        }

        /// <summary>
        /// Get a player profile from Firebase (on-demand loading)
        /// </summary>
        public async Task<PlayerProfile?> GetPlayerAsync(string playerName)
        {
            try
            {
                return await _firebaseService.GetPlayerProfileAsync(playerName);
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception getting player {playerName}: {ex.Message}", "PlayerManager", ex);
                return null;
            }
        }

        /// <summary>
        /// Get all players (loads from Firebase on-demand)
        /// </summary>
        public async Task<List<PlayerProfile>> GetAllPlayersAsync()
        {
            try
            {
                return await _firebaseService.GetAllPlayerProfilesAsync();
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception getting all players: {ex.Message}", "PlayerManager", ex);
                return new List<PlayerProfile>();
            }
        }

        /// <summary>
        /// Check if a player exists in Firebase
        /// </summary>
        public async Task<bool> PlayerExistsAsync(string playerName)
        {
            try
            {
                var player = await _firebaseService.GetPlayerProfileAsync(playerName);
                return player != null;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception checking if player {playerName} exists: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Get a player by name (synchronous version for backward compatibility)
        /// </summary>
        public PlayerProfile? GetPlayerByName(string name)
        {
            return _players.FirstOrDefault(p => 
                string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get active players from local collection
        /// </summary>
        public List<PlayerProfile> GetActivePlayers()
        {
            return _players.Where(p => p.IsActive).ToList();
        }

        // REMOVED: UpdatePlayerSessionStatsAsync method - no longer needed since lifetime totals are only updated when sessions end
        // public async Task<bool> UpdatePlayerSessionStatsAsync(string playerName, double buyIn, double cashOut)
        // {
        //     try
        //     {
        //         var player = await GetPlayerAsync(playerName);
        //         if (player != null)
        //         {
        //             player.AddSessionStats(buyIn, cashOut);
        //             return await UpdatePlayerAsync(player);
        //         }
        //         return false;
        //     }
        //     catch (Exception ex)
        //     {
        //         LogToFile($"Exception updating player session stats for {playerName}: {ex.Message}");
        //         return false;
        //     }
        // }

        /// <summary>
        /// Search players by search term
        /// </summary>
        public List<PlayerProfile> SearchPlayers(string searchTerm)
        {
            if (string.IsNullOrWhiteSpace(searchTerm))
                return _players.ToList();

            var term = searchTerm.ToLower();
            return _players.Where(p => 
                p.SearchText.Contains(term) || 
                p.Name.ToLower().Contains(term) ||
                p.Nickname.ToLower().Contains(term)
            ).ToList();
        }

        /// <summary>
        /// Clear corrupted session data and recalculate totals from actual session data
        /// </summary>
        public async Task<bool> FixCorruptedPlayerDataAsync(string playerName)
        {
            try
            {
                LoggingService.Instance.Info($"Fixing corrupted data for player: {playerName}", "PlayerManager");
                
                var player = await GetPlayerAsync(playerName);
                if (player == null)
                {
                    LoggingService.Instance.Warning($"Player {playerName} not found", "PlayerManager");
                    return false;
                }

                // Clear all corrupted session data
                player.ClearSessionData();
                
                // Recalculate totals from actual session data
                player.RecalculateTotalsFromSessions();
                
                // Save the fixed player profile
                var success = await UpdatePlayerAsync(player);
                if (success)
                {
                    LoggingService.Instance.Info($"Successfully fixed corrupted data for player: {playerName}", "PlayerManager");
                    LoggingService.Instance.Info($"New totals: Sessions={player.TotalSessionsPlayed}, BuyIns=${player.TotalLifetimeBuyIn:F2}, CashOuts=${player.TotalLifetimeCashOut:F2}", "PlayerManager");
                }
                else
                {
                    LoggingService.Instance.Error($"Failed to save fixed data for player: {playerName}", "PlayerManager");
                }
                
                return success;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception fixing corrupted data for player {playerName}: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Fix all players with corrupted session data
        /// </summary>
        public async Task<bool> FixAllCorruptedPlayerDataAsync()
        {
            try
            {
                LoggingService.Instance.Info("Starting to fix all corrupted player data...", "PlayerManager");
                
                var allPlayers = await GetAllPlayersAsync();
                var successCount = 0;
                var totalCount = allPlayers.Count;
                
                foreach (var player in allPlayers)
                {
                    if (await FixCorruptedPlayerDataAsync(player.Name))
                    {
                        successCount++;
                    }
                }
                
                LoggingService.Instance.Info($"Fixed corrupted data for {successCount}/{totalCount} players", "PlayerManager");
                return successCount == totalCount;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception fixing all corrupted player data: {ex.Message}", "PlayerManager", ex);
                return false;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
} 