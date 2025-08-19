using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Linq;
using System.IO;
using System.Text.Json;
using PokerTracker2.Models;
using PokerTracker2.Services;
using System.Threading.Tasks;

namespace PokerTracker2.Services
{
    public class SessionManager : INotifyPropertyChanged
    {
        private Session? _currentSession;
        private readonly ObservableCollection<Player> _players;
        private readonly ObservableCollection<Transaction> _buyInLog;
        private readonly Dictionary<string, double> _cashOuts;
        private readonly ObservableCollection<Session> _allSessions;
        private readonly PlayerManager _playerManager;
        private readonly FirebaseService _firebaseService;
        private string? _pendingActiveSessionId; // Added for pending active session loading



        public Session? CurrentSession
        {
            get => _currentSession;
            set => SetProperty(ref _currentSession, value);
        }

        public ObservableCollection<Player> Players => _players;
        public ObservableCollection<Transaction> BuyInLog => _buyInLog;
        public Dictionary<string, double> CashOuts => _cashOuts;
        public ObservableCollection<Session> AllSessions => _allSessions;

        public string CurrentSessionId => CurrentSession?.Id ?? string.Empty;
        public bool HasActiveSession => CurrentSession != null;

        public int TotalSessions => _allSessions.Count;
        public int ActiveSessions => _allSessions.Count(s => s.EndTime == DateTime.MinValue);
        
        // REMOVED: TotalNetProfit property - poker sessions should always balance to 0
        // public double TotalNetProfit => _allSessions.Sum(s => s.NetProfit);
        
        public SessionManager(PlayerManager playerManager)
        {
            _playerManager = playerManager;
            _firebaseService = new FirebaseService();
            _players = new ObservableCollection<Player>();
            _buyInLog = new ObservableCollection<Transaction>();
            _cashOuts = new Dictionary<string, double>();
            _allSessions = new ObservableCollection<Session>();
            
            // No more local file storage - everything goes to Firebase
            // LoadAllSessions(); // REMOVED - prevents loading contaminated local data
        }

        /// <summary>
        /// Initialize SessionManager by loading sessions from Firebase
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                LoggingService.Instance.Info("Initializing SessionManager - loading sessions from Firebase", "SessionManager");
                var success = await LoadSessionsFromFirebaseAsync();
                
                if (success)
                {
                    LoggingService.Instance.Info($"Successfully loaded {_allSessions.Count} sessions from Firebase", "SessionManager");
                    
                    // Check if there are any active sessions (sessions without end times)
                    var activeSessions = _allSessions.Where(s => s.EndTime == DateTime.MinValue).ToList();
                    if (activeSessions.Count > 0)
                    {
                        // Just mark that we found an active session - don't load it here
                        // The UI thread will handle loading it after initialization
                        var mostRecentActive = activeSessions.OrderByDescending(s => s.StartTime).First();
                        LoggingService.Instance.Info($"Found active session: {mostRecentActive.Name} - will be loaded by UI thread", "SessionManager");
                        
                        // Store the session ID for the UI thread to load
                        _pendingActiveSessionId = mostRecentActive.Id;
                    }
                    
                    return true;
                }
                else
                {
                    LoggingService.Instance.Error("Failed to load sessions from Firebase", "SessionManager");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception during SessionManager initialization: {ex.Message}", "SessionManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Get the ID of the pending active session that was found during initialization
        /// </summary>
        public string? GetPendingActiveSessionId()
        {
            return _pendingActiveSessionId;
        }

        /// <summary>
        /// Load the pending active session (should be called from UI thread)
        /// </summary>
        public async Task LoadPendingActiveSession()
        {
            if (string.IsNullOrEmpty(_pendingActiveSessionId)) return;
            
            var session = _allSessions.FirstOrDefault(s => s.Id == _pendingActiveSessionId);
            if (session != null)
            {
                LoggingService.Instance.Info($"Loading pending active session: {session.Name}", "SessionManager");
                await LoadSession(session);
                _pendingActiveSessionId = null; // Clear after loading
            }
        }

        public void CreateNewSession()
        {
            var sessionName = GenerateSessionName();
            CurrentSession = new Session(sessionName);
            CurrentSession.StartTime = DateTime.Now;
            _players.Clear();
            _buyInLog.Clear();
            _cashOuts.Clear();
            
            // Add to all sessions
            _allSessions.Add(CurrentSession);
            
            OnPropertyChanged(nameof(CurrentSessionId));
            OnPropertyChanged(nameof(HasActiveSession));
            OnPropertyChanged(nameof(TotalSessions));
            OnPropertyChanged(nameof(ActiveSessions));
            
            LoggingService.Instance.Info($"Created new session: {sessionName}", "SessionManager");
        }

        /// <summary>
        /// Creates a session template for configuration without adding it to the sessions list
        /// This is used for the NewSessionPage before the user saves the session
        /// </summary>
        public void CreateSessionTemplate()
        {
            var sessionName = GenerateSessionName();
            CurrentSession = new Session(sessionName);
            CurrentSession.StartTime = DateTime.Now;
            _players.Clear();
            _buyInLog.Clear();
            _cashOuts.Clear();
            
            // NOTE: Do NOT add to _allSessions here - this is just a template
            // The session will only be added when SaveSession() is called
            
            OnPropertyChanged(nameof(CurrentSessionId));
            OnPropertyChanged(nameof(HasActiveSession));
            
            LoggingService.Instance.Info($"Created session template: {sessionName}", "SessionManager");
        }

        /// <summary>
        /// Saves the current session template and adds it to the sessions list
        /// This should only be called when the user has configured the session and wants to save it
        /// </summary>
        public async Task<bool> SaveSessionTemplate()
        {
            if (CurrentSession != null)
            {
                // Save the session data to Firebase
                var success = await SaveSession(false); // Pass false - template saves should not finalize sessions
                if (success)
                {
                                    LoggingService.Instance.Info($"Saved session template to Firebase: {CurrentSession.Name}", "SessionManager");
                return true;
            }
            else
            {
                LoggingService.Instance.Error($"Failed to save session template to Firebase: {CurrentSession.Name}", "SessionManager");
                return false;
            }
            }
            return false;
        }

        private string GenerateSessionName()
        {
            var now = DateTime.Now;
            var dayOfWeek = now.ToString("dddd");
            var time = now.ToString("HH:mm");
            var date = now.ToString("MMM dd");
            
            return $"{dayOfWeek} {date} at {time}";
        }

        public void RenameCurrentSession(string newName)
        {
            if (CurrentSession != null && !string.IsNullOrWhiteSpace(newName))
            {
                CurrentSession.Name = newName.Trim();
                CurrentSession.UpdatedAt = DateTime.Now;
                OnPropertyChanged(nameof(CurrentSessionId));
                // SaveAllSessions(); // REMOVED - no more local file storage
                LoggingService.Instance.Info($"Renamed session to: {newName}", "SessionManager");
            }
        }

        public async Task LoadSession(Session session)
        {
            if (session == null) return;
            
            CurrentSession = session;
            
            // Load session data
            _players.Clear();
            _buyInLog.Clear();
            _cashOuts.Clear();
            
            // Load players and transactions from session
            if (session.Players != null)
            {
                foreach (var player in session.Players)
                {
                    _players.Add(player);
                }
            }
            
            if (session.Transactions != null)
            {
                foreach (var transaction in session.Transactions)
                {
                    _buyInLog.Add(transaction);
                    
                    if (transaction.Type == TransactionType.CashOut)
                    {
                        _cashOuts[transaction.PlayerName] = transaction.Amount;
                    }
                }
            }
            
            OnPropertyChanged(nameof(CurrentSessionId));
            OnPropertyChanged(nameof(HasActiveSession));
            OnPropertyChanged(nameof(TotalBuyIn));
            OnPropertyChanged(nameof(TotalCashOut));
            OnPropertyChanged(nameof(IsSessionBalanced));
            
            // Initialize recent activity and profile data for all loaded players
            foreach (var player in _players)
            {
                // For existing sessions, we need to migrate transaction data to the enhanced model
                await MigratePlayerTransactionHistory(player);
                
                UpdatePlayerRecentActivity(player.Name);
                await LoadPlayerProfileDataAsync(player.Name);
            }
            
            // Populate buy-in graph data for all players
            PopulateBuyInGraphData();
            
            LoggingService.Instance.Info($"Loaded session: {session.Name}", "SessionManager");
        }

        public async Task<bool> SaveSession(bool isFinalSave = false)
        {
            if (CurrentSession == null) return false;
            
            try
            {
                // Update session with current data
                CurrentSession.Players = _players.ToList();
                CurrentSession.Transactions = _buyInLog.ToList();
                CurrentSession.UpdatedAt = DateTime.UtcNow;
                
                // Calculate session totals
                CurrentSession.TotalBuyIns = _buyInLog.Where(t => t.Type == TransactionType.BuyIn).Sum(t => t.Amount);
                CurrentSession.TotalCashOuts = _cashOuts.Values.Sum();
                // REMOVED: NetProfit calculation - poker sessions should always balance to 0
                // CurrentSession.NetProfit = CurrentSession.TotalCashOuts - CurrentSession.TotalBuyIns;
                
                // Update player statistics in Firebase
                await UpdatePlayerStatistics(isFinalSave); // Pass the correct isFinalSave value
                
                // Save session to Firebase
                var success = await _firebaseService.SaveSessionAsync(CurrentSession);
                if (success)
                {
                    LoggingService.Instance.Info($"Saved session to Firebase: {CurrentSession.Name} (isFinalSave: {isFinalSave})", "SessionManager");
                    
                    // Add to local collection if not already there
                    if (!_allSessions.Contains(CurrentSession))
                    {
                        _allSessions.Add(CurrentSession);
                        OnPropertyChanged(nameof(TotalSessions));
                        OnPropertyChanged(nameof(ActiveSessions));
                    }
                    
                    return true;
                }
                else
                {
                                    LoggingService.Instance.Error($"Failed to save session to Firebase: {CurrentSession.Name}", "SessionManager");
                return false;
            }
        }
        catch (Exception ex)
        {
            LoggingService.Instance.Error($"Exception saving session: {ex.Message}", "SessionManager", ex);
            return false;
        }
        }

        public async Task<bool> EndSession()
        {
            if (CurrentSession == null) return false;
            
            try
            {
                CurrentSession.EndTime = DateTime.UtcNow;
                var success = await SaveSession(true); // Pass true for ending the session
                
                if (success)
                {
                    // Clear current session
                    CurrentSession = null;
                    _players.Clear();
                    _buyInLog.Clear();
                    _cashOuts.Clear();
                    
                    OnPropertyChanged(nameof(CurrentSessionId));
                    OnPropertyChanged(nameof(HasActiveSession));
                    
                    LoggingService.Instance.Info($"Ended session and saved to Firebase", "SessionManager");
                    return true;
                }
                else
                {
                    LoggingService.Instance.Error($"Failed to save ended session to Firebase", "SessionManager");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception ending session: {ex.Message}", "SessionManager", ex);
                return false;
            }
        }

        public List<Session> GetActiveSessions()
        {
            return _allSessions.Where(s => s.EndTime == DateTime.MinValue).ToList();
        }

        public List<Session> GetCompletedSessions()
        {
            return _allSessions.Where(s => s.EndTime != DateTime.MinValue).ToList();
        }

        public List<Session> GetRecentSessions(int count = 5)
        {
            return _allSessions.OrderByDescending(s => s.StartTime).Take(count).ToList();
        }

        /// <summary>
        /// Gets all sessions that a specific player participated in
        /// </summary>
        public List<Session> GetPlayerSessions(string playerName)
        {
            return _allSessions.Where(s => 
                s.Players != null && 
                s.Players.Any(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase))
            ).ToList();
        }

        /// <summary>
        /// Gets player-specific session statistics
        /// </summary>
        public PlayerSessionStats GetPlayerSessionStats(string playerName)
        {
            var playerSessions = GetPlayerSessions(playerName);
            var completedSessions = playerSessions.Where(s => s.EndTime != DateTime.MinValue).ToList();
            
            var stats = new PlayerSessionStats
            {
                PlayerName = playerName,
                TotalSessions = playerSessions.Count,
                CompletedSessions = completedSessions.Count,
                ActiveSessions = playerSessions.Count - completedSessions.Count,
                TotalBuyIns = completedSessions.Sum(s => s.Players?.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase))?.TotalBuyIn ?? 0),
                TotalCashOuts = completedSessions.Sum(s => s.Players?.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase))?.TotalCashOut ?? 0),
                // REMOVED: NetProfit calculation - poker sessions should always balance to 0
                // NetProfit = completedSessions.Sum(s => 
                // {
                //     var player = s.Players?.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase));
                //     if (player != null)
                //     {
                //         return (player.TotalCashOut + player.CurrentStack) - player.TotalBuyIn;
                //     }
                //     return 0;
                // }),
                AverageProfit = completedSessions.Count > 0 ? 
                    completedSessions.Sum(s => 
                    {
                        var player = s.Players?.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase));
                        if (player != null)
                        {
                            return (player.TotalCashOut + player.CurrentStack) - player.TotalBuyIn;
                        }
                        return 0;
                    }) / completedSessions.Count : 0,
                BestSession = completedSessions.Count > 0 ? 
                    completedSessions.Max(s => 
                    {
                        var player = s.Players?.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase));
                        if (player != null)
                        {
                            return (player.TotalCashOut + player.CurrentStack) - player.TotalBuyIn;
                        }
                        return 0;
                    }) : 0,
                WorstSession = completedSessions.Count > 0 ? 
                    completedSessions.Min(s => 
                    {
                        var player = s.Players?.FirstOrDefault(p => string.Equals(p.Name, playerName, StringComparison.OrdinalIgnoreCase));
                        if (player != null)
                        {
                            return (player.TotalCashOut + player.CurrentStack) - player.TotalBuyIn;
                        }
                        return 0;
                    }) : 0,
                LastPlayed = completedSessions.Count > 0 ? completedSessions.Max(s => s.EndTime) : DateTime.MinValue
            };
            
            return stats;
        }

        /// <summary>
        /// Gets player-specific recent sessions
        /// </summary>
        public List<Session> GetPlayerRecentSessions(string playerName, int count = 5)
        {
            return GetPlayerSessions(playerName)
                .OrderByDescending(s => s.StartTime)
                .Take(count)
                .ToList();
        }

        private void LoadAllSessions()
        {
            try
            {
                // This method is no longer needed as all sessions are loaded from Firebase
                // if (File.Exists(_sessionsFilePath))
                // {
                //     string json = File.ReadAllText(_sessionsFilePath);
                //     var sessions = JsonSerializer.Deserialize<List<Session>>(json);
                    
                //     if (sessions != null)
                //     {
                //         _allSessions.Clear();
                //         foreach (var session in sessions)
                //         {
                //             _allSessions.Add(session);
                //         }
                //     }
                    
                //     LogToFile($"Loaded {_allSessions.Count} sessions from file");
                // }
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error loading sessions: {ex.Message}", "SessionManager", ex);
            }
        }

        /// <summary>
        /// Load sessions from Firebase to populate local collection
        /// </summary>
        public async Task<bool> LoadSessionsFromFirebaseAsync()
        {
            try
            {
                LoggingService.Instance.Info("LoadSessionsFromFirebaseAsync started", "SessionManager");
                var sessions = await _firebaseService.GetRecentSessionsAsync(365); // Last year
                LoggingService.Instance.Info($"Retrieved {sessions.Count} sessions from Firebase", "SessionManager");
                
                _allSessions.Clear();
                foreach (var session in sessions)
                {
                    _allSessions.Add(session);
                }
                
                // Don't call OnPropertyChanged here - it will be called from the UI thread after initialization
                // OnPropertyChanged(nameof(TotalSessions));
                // OnPropertyChanged(nameof(ActiveSessions));
                
                LoggingService.Instance.Info($"Loaded {_allSessions.Count} sessions from Firebase", "SessionManager");
                LoggingService.Instance.Info($"TotalSessions property will now return: {TotalSessions}", "SessionManager");
                LoggingService.Instance.Info($"ActiveSessions property will now return: {ActiveSessions}", "SessionManager");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Error loading sessions from Firebase: {ex.Message}", "SessionManager", ex);
                return false;
            }
        }

        /// <summary>
        /// Delete a session from both local collection and Firebase
        /// </summary>
        public async Task<bool> DeleteSessionAsync(Session session)
        {
            try
            {
                // Delete from Firebase first
                var firebaseSuccess = await _firebaseService.DeleteSessionAsync(session.Id);
                if (!firebaseSuccess)
                {
                    LoggingService.Instance.Error($"Failed to delete session {session.Id} from Firebase", "SessionManager");
                    return false;
                }

                // Remove from local collection
                _allSessions.Remove(session);
                
                // Update player statistics since this session is being removed
                await UpdatePlayerStatistics(false);
                
                LoggingService.Instance.Info($"Successfully deleted session {session.Id} from both Firebase and local collection", "SessionManager");
                return true;
            }
            catch (Exception ex)
            {
                LoggingService.Instance.Error($"Exception deleting session: {ex.Message}", "SessionManager", ex);
                return false;
            }
        }

        public async void AddPlayer(string name, double buyInAmount)
        {
            // Check if player already exists
            var existingPlayer = _players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            
            if (existingPlayer != null)
            {
                // Player exists - add buy-in using enhanced transaction system
                existingPlayer.AddBuyIn(buyInAmount, DateTime.Now, "Additional buy-in");
                
                // If player has a final stack set, increase it by the buy-in amount
                // because they're adding more chips to the table
                if (existingPlayer.FinalStack.HasValue)
                {
                    existingPlayer.FinalStack = existingPlayer.FinalStack.Value + buyInAmount;
                    LoggingService.Instance.Info($"Increased {name}'s final stack by ${buyInAmount} due to additional buy-in (new final stack: ${existingPlayer.FinalStack.Value})", "SessionManager");
                }
                
                existingPlayer.LastActivityTime = DateTime.Now;
                LoggingService.Instance.Info($"Updated existing player {name} - added ${buyInAmount} buy-in (total now: ${existingPlayer.TotalBuyIn})", "SessionManager");
            }
            else
            {
                // Get player profile to link with session player
                var playerProfile = await _playerManager.GetPlayerAsync(name);
                var profileId = playerProfile?.Name ?? name; // Use name as fallback ID
                
                // New player - create enhanced Player object with ProfileId
                var player = new Player(name, profileId, buyInAmount);
                
                // Link to profile for read-only access during session
                player.Profile = playerProfile;
                
                // Set session ID for transaction tracking
                var sessionId = CurrentSession?.Id ?? "";
                
                // The constructor already added the initial buy-in to transaction history
                // Just set the session ID for the existing transaction
                if (buyInAmount > 0 && player.TransactionHistory.Count > 0)
                {
                    var initialTransaction = player.TransactionHistory.FirstOrDefault(t => t.Type == TransactionType.BuyIn);
                    if (initialTransaction != null)
                    {
                        initialTransaction.SessionId = sessionId;
                    }
                }
                
                _players.Add(player);
                LoggingService.Instance.Info($"Added new player {name} (ProfileId: {profileId}) with buy-in ${buyInAmount}", "SessionManager");
            }

            // Still maintain backward compatibility with session-level transaction log
            var transaction = new Transaction(name, buyInAmount, TransactionType.BuyIn);
            _buyInLog.Add(transaction);

            OnPropertyChanged(nameof(TotalBuyIn));
            OnPropertyChanged(nameof(TotalFinalStacks));
            OnPropertyChanged(nameof(IsSessionBalanced));
            
            // Load profile data for this player (now using ProfileId)
            await LoadPlayerProfileDataAsync(name);
        }

        public void AddPlayerToActiveSession(string name, double buyInAmount)
        {
            if (CurrentSession == null || !CurrentSession.IsActive)
            {
                LoggingService.Instance.Warning($"Cannot add player {name} - no active session", "SessionManager");
                return;
            }

            AddPlayer(name, buyInAmount);
                            LoggingService.Instance.Info($"Added player {name} to active session with buy-in ${buyInAmount}", "SessionManager");
        }

        public void RemovePlayer(string name)
        {
            if (CurrentSession == null)
            {
                LoggingService.Instance.Warning("Cannot remove player - no active session", "SessionManager");
                return;
            }

            var playerToRemove = _players.FirstOrDefault(p => p.Name.Equals(name, StringComparison.OrdinalIgnoreCase));
            if (playerToRemove != null)
            {
                _players.Remove(playerToRemove);
                
                // Also remove any transactions for this player
                for (int i = _buyInLog.Count - 1; i >= 0; i--)
                {
                    if (_buyInLog[i].PlayerName.Equals(name, StringComparison.OrdinalIgnoreCase))
                    {
                        _buyInLog.RemoveAt(i);
                    }
                }
                _cashOuts.Remove(name);
                
                OnPropertyChanged(nameof(TotalBuyIn));
                OnPropertyChanged(nameof(TotalFinalStacks));
                OnPropertyChanged(nameof(IsSessionBalanced));
                
                LoggingService.Instance.Info($"Removed player {name} from session", "SessionManager");
            }
            else
            {
                LoggingService.Instance.Warning($"Player {name} not found in current session", "SessionManager");
            }
        }

        public void AddCashOut(string playerName, double amount)
        {
            // Update the SessionManager's cash-out tracking (backward compatibility)
            if (_cashOuts.ContainsKey(playerName))
            {
                _cashOuts[playerName] += amount;
            }
            else
            {
                _cashOuts[playerName] = amount;
            }

            // Update the Player object using enhanced transaction system
            var existingPlayer = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (existingPlayer != null)
            {
                // Add cash-out using enhanced transaction system
                existingPlayer.AddCashOut(amount, DateTime.Now, "Cash-out");
                
                // Set session ID for the transaction
                var sessionId = CurrentSession?.Id ?? "";
                var lastTransaction = existingPlayer.TransactionHistory.LastOrDefault();
                if (lastTransaction != null)
                {
                    lastTransaction.SessionId = sessionId;
                }
                
                // If player has a final stack set, reduce it by the cash-out amount
                // because those chips are coming off the table
                if (existingPlayer.FinalStack.HasValue)
                {
                    existingPlayer.FinalStack = existingPlayer.FinalStack.Value - amount;
                    LoggingService.Instance.Info($"Reduced {playerName}'s final stack by ${amount} due to cash-out (new final stack: ${existingPlayer.FinalStack.Value})", "SessionManager");
                }
                
                existingPlayer.LastActivityTime = DateTime.Now;
                LoggingService.Instance.Info($"Updated player {playerName} - added ${amount} cash-out (total now: ${existingPlayer.TotalCashOut})", "SessionManager");
            }
            else
            {
                LoggingService.Instance.Warning($"Could not find player {playerName} to update cash-out", "SessionManager");
            }

            // Still maintain backward compatibility with session-level transaction log
            var transaction = new Transaction(playerName, amount, TransactionType.CashOut);
            _buyInLog.Add(transaction);

            OnPropertyChanged(nameof(TotalCashOut));
            OnPropertyChanged(nameof(TotalFinalStacks));
            OnPropertyChanged(nameof(IsSessionBalanced));
            
            LoggingService.Instance.Info($"Added cash-out for {playerName}: ${amount}", "SessionManager");
        }

        public double GetPlayerBuyIn(string playerName)
        {
            return _buyInLog.Where(t => t.PlayerName == playerName && t.Type == TransactionType.BuyIn)
                           .Sum(t => t.Amount);
        }

        public double GetPlayerCashOut(string playerName)
        {
            return _cashOuts.ContainsKey(playerName) ? _cashOuts[playerName] : 0;
        }

        public void SetPlayerFinalStack(string playerName, double finalStack)
        {
            var existingPlayer = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (existingPlayer != null)
            {
                // Update the player's final stack
                existingPlayer.FinalStack = finalStack;
                existingPlayer.LastActivityTime = DateTime.Now;
                
                // Clear any existing cash-out data for this player since final stacks are not cash-outs
                if (_cashOuts.ContainsKey(playerName))
                {
                    _cashOuts.Remove(playerName);
                }
                
                // Reset the player's TotalCashOut property to 0
                existingPlayer.TotalCashOut = 0;
                
                LoggingService.Instance.Info($"Set final stack for {playerName}: ${finalStack} (cleared cash-out data)", "SessionManager");
                
                // Notify that session balance may have changed
                OnPropertyChanged(nameof(TotalCashOut));
                OnPropertyChanged(nameof(TotalFinalStacks));
                OnPropertyChanged(nameof(IsSessionBalanced));
            }
            else
            {
                LoggingService.Instance.Warning($"Could not find player {playerName} to set final stack", "SessionManager");
            }
        }

        public ObservableCollection<Player> GetPlayers()
        {
            return _players;
        }

        public List<Transaction> GetPlayerTransactions(string playerName)
        {
            return _buyInLog.Where(t => t.PlayerName == playerName)
                           .OrderBy(t => t.Timestamp)
                           .ToList();
        }

        public void UpdatePlayerRecentActivity(string playerName)
        {
            var player = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                // The enhanced Player model automatically updates recent activity
                // through the UpdateRecentActivity() method when transactions are added
                // This method is now mostly for backward compatibility
                
                // For additional safety, we can also check the player's own transaction history
                var recentTransactions = player.TransactionHistory
                    .OrderByDescending(t => t.Timestamp)
                    .Take(2)
                    .ToList();
                
                if (recentTransactions.Count > 0)
                {
                    player.RecentActivity1 = $"• {recentTransactions[0].Type}: ${recentTransactions[0].Amount:F2} at {recentTransactions[0].Timestamp:HH:mm}";
                    
                    if (recentTransactions.Count > 1)
                    {
                        player.RecentActivity2 = $"• {recentTransactions[1].Type}: ${recentTransactions[1].Amount:F2} at {recentTransactions[1].Timestamp:HH:mm}";
                    }
                    else
                    {
                        player.RecentActivity2 = "";
                    }
                }
                else
                {
                    player.RecentActivity1 = "• No recent activity";
                    player.RecentActivity2 = "";
                }
                
                // Property setters will automatically trigger change notifications
            }
        }
        
        public void LoadPlayerProfileData(string playerName)
        {
            // Keep synchronous version for backward compatibility
            var player = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                var profile = _playerManager.GetPlayerByName(playerName);
                if (profile != null)
                {
                    player.ProfileCreatedDate = profile.CreatedDate;
                    player.ProfileLastPlayedDate = profile.LastPlayedDate;
                }
            }
        }

        public async Task LoadPlayerProfileDataAsync(string playerName)
        {
            var player = _players.FirstOrDefault(p => p.Name.Equals(playerName, StringComparison.OrdinalIgnoreCase));
            if (player != null)
            {
                // Get profile using async method for more complete data
                var profile = await _playerManager.GetPlayerAsync(playerName);
                if (profile != null)
                {
                    // Update profile reference for read-only access
                    player.Profile = profile;
                    
                    // Update profile timeline data
                    player.ProfileCreatedDate = profile.CreatedDate;
                    player.ProfileLastPlayedDate = profile.LastPlayedDate;
                    
                    // Update ProfileId if it wasn't set correctly
                    if (string.IsNullOrEmpty(player.ProfileId))
                    {
                        player.ProfileId = profile.Name;
                    }
                    
                    LoggingService.Instance.Debug($"Loaded profile data for {playerName} - ProfileId: {player.ProfileId}", "SessionManager");
                }
                else
                {
                    LoggingService.Instance.Warning($"Could not find profile for player {playerName}", "SessionManager");
                }
            }
        }

        /// <summary>
        /// Migrates transaction data from session-level logs to player-level transaction history
        /// This is needed for backward compatibility with existing sessions
        /// </summary>
        private async Task MigratePlayerTransactionHistory(Player player)
        {
            // Skip if player already has transaction history (new enhanced model)
            if (player.TransactionHistory.Count > 0) return;
            
            var sessionId = CurrentSession?.Id ?? "";
            
            // Get all transactions for this player from session-level log
            var playerTransactions = _buyInLog
                .Where(t => t.PlayerName.Equals(player.Name, StringComparison.OrdinalIgnoreCase))
                .OrderBy(t => t.Timestamp)
                .ToList();
            
            LoggingService.Instance.Info($"Migrating {playerTransactions.Count} transactions for player {player.Name}", "SessionManager");
            
            // Migrate each transaction to the enhanced model
            foreach (var transaction in playerTransactions)
            {
                var playerTransaction = new PlayerTransaction
                {
                    TransactionId = Guid.NewGuid().ToString(),
                    Type = transaction.Type,
                    Amount = transaction.Amount,
                    Timestamp = transaction.Timestamp,
                    Note = $"Migrated from session log - {transaction.Notes}",
                    SessionId = sessionId
                };
                
                player.TransactionHistory.Add(playerTransaction);
            }
            
            // Recalculate totals by updating the properties manually
            // The Player model will automatically calculate totals from transaction history
            var calculatedBuyIn = player.CalculatedTotalBuyIn;
            var calculatedCashOut = player.CalculatedTotalCashOut;
            
            // Update the stored totals to match calculated totals
            if (calculatedBuyIn != player.TotalBuyIn)
            {
                player.TotalBuyIn = calculatedBuyIn;
            }
            if (calculatedCashOut != player.TotalCashOut)
            {
                player.TotalCashOut = calculatedCashOut;
            }
            
            // Ensure ProfileId is set
            if (string.IsNullOrEmpty(player.ProfileId))
            {
                var profile = await _playerManager.GetPlayerAsync(player.Name);
                player.ProfileId = profile?.Name ?? player.Name;
                player.Profile = profile;
            }
            
            // Validate transaction integrity
            var isValid = player.ValidateTransactionIntegrity();
            if (!isValid)
            {
                LoggingService.Instance.Warning($"Transaction integrity validation failed for player {player.Name} after migration", "SessionManager");
            }
            
            LoggingService.Instance.Info($"Migrated transaction history for {player.Name} - ProfileId: {player.ProfileId}, Transactions: {player.TransactionHistory.Count}", "SessionManager");
        }

        public double TotalBuyIn => _buyInLog.Where(t => t.Type == TransactionType.BuyIn).Sum(t => t.Amount);
        public double TotalCashOut => _cashOuts.Values.Sum();
        public double TotalFinalStacks => _players.Sum(p => p.FinalStack ?? 0);
        public double TotalCurrentStacks => _players.Sum(p => p.CurrentStack);
        public bool IsSessionBalanced => Math.Abs(TotalBuyIn - TotalCurrentStacks) < 0.01;

        private async Task UpdatePlayerStatistics(bool isFinalSave = false)
        {
            // Update player statistics for each player in the session
            foreach (var player in _players)
            {
                var playerBuyIn = GetPlayerBuyIn(player.Name);
                var playerCashOut = GetPlayerCashOut(player.Name);
                
                // Get the player profile to update
                var playerProfile = await _playerManager.GetPlayerAsync(player.Name);
                if (playerProfile != null)
                {
                    // Check if this session is already referenced in the player profile
                    var sessionAlreadyReferenced = playerProfile.SessionIds?.Contains(CurrentSession?.Id ?? "") ?? false;
                    
                    if (!sessionAlreadyReferenced && CurrentSession != null)
                    {
                        // Only add session reference if this is a new session for this player
                        var sessionDuration = CurrentSession.StartTime != DateTime.MinValue 
                            ? DateTime.UtcNow - CurrentSession.StartTime 
                            : TimeSpan.Zero;
                        
                        playerProfile.AddSessionReference(
                            CurrentSession.Id,
                            CurrentSession.Name,
                            CurrentSession.StartTime,
                            playerBuyIn,
                            playerCashOut,
                            sessionDuration,
                            _players.Count
                        );
                        
                        LoggingService.Instance.Info($"Added new session reference for {player.Name}: {CurrentSession.Name}", "SessionManager");
                    }
                    else if (sessionAlreadyReferenced && CurrentSession != null)
                    {
                        // Update existing session reference with current data
                        var existingSession = playerProfile.RecentSessions?.FirstOrDefault(s => s.SessionId == CurrentSession.Id);
                        if (existingSession != null)
                        {
                            // Calculate the additional buy-in/cash-out since last update
                            var additionalBuyIn = playerBuyIn - existingSession.BuyIn;
                            var additionalCashOut = playerCashOut - existingSession.CashOut;
                            
                            // Use UpdateSessionReference to properly track changes
                            playerProfile.UpdateSessionReference(CurrentSession.Id, additionalBuyIn, additionalCashOut);
                            
                            LoggingService.Instance.Info($"Updated existing session reference for {player.Name}: {CurrentSession.Name} (Additional: BuyIn=${additionalBuyIn}, CashOut=${additionalCashOut})", "SessionManager");
                        }
                    }
                    
                    // Only finalize the session (add to lifetime totals) when it's actually ended
                    if (isFinalSave && CurrentSession != null)
                    {
                        // Finalize the session for this player
                        var totalSessionBuyIns = player.TotalBuyIn;
                        var totalSessionCashOuts = player.TotalCashOut + (player.FinalStack ?? 0);
                        var sessionDuration = CurrentSession.StartTime != DateTime.MinValue 
                            ? DateTime.UtcNow - CurrentSession.StartTime 
                            : TimeSpan.Zero;
                        
                        playerProfile.FinalizeSession(
                            CurrentSession.Id, 
                            totalSessionBuyIns, 
                            totalSessionCashOuts,
                            CurrentSession.Name,
                            CurrentSession.StartTime,
                            sessionDuration,
                            _players.Count
                        );
                    }
                    
                    // Update the player profile in Firebase
                    await _playerManager.UpdatePlayerAsync(playerProfile);
                }
                
                // REMOVED: This was still updating lifetime totals prematurely
                // await _playerManager.UpdatePlayerSessionStatsAsync(player.Name, playerBuyIn, playerCashOut);
            }
        }
        
        // Method to populate buy-in graph data for all players from transaction history
        public void PopulateBuyInGraphData()
        {
            System.Diagnostics.Debug.WriteLine($"PopulateBuyInGraphData called for {_players.Count} players");
            
            foreach (var player in _players)
            {
                // Clear existing graph data
                player.BuyInGraphData.Clear();
                
                // Get all buy-in transactions for this player, ordered by timestamp
                var buyInTransactions = _buyInLog
                    .Where(t => t.PlayerName.Equals(player.Name, StringComparison.OrdinalIgnoreCase) && t.Type == TransactionType.BuyIn)
                    .OrderBy(t => t.Timestamp)
                    .ToList();
                
                System.Diagnostics.Debug.WriteLine($"Player {player.Name}: Found {buyInTransactions.Count} buy-in transactions");
                
                foreach (var transaction in buyInTransactions)
                {
                    player.AddBuyInPoint(transaction.Amount, transaction.Timestamp);
                }
                
                var finalAmount = player.BuyInGraphData.Count > 0 ? player.BuyInGraphData[player.BuyInGraphData.Count - 1].Amount : 0;
                System.Diagnostics.Debug.WriteLine($"Player {player.Name}: Final BuyInGraphData.Count = {player.BuyInGraphData.Count}, final amount: ${finalAmount}");
                LoggingService.Instance.Debug($"Populated buy-in graph data for {player.Name}: {buyInTransactions.Count} transactions, total: ${finalAmount}", "SessionManager");
            }
        }

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
    }
} 