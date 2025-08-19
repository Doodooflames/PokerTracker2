using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.IO;
using System.Text.Json;
using Google.Cloud.Firestore;
using Google.Apis.Auth.OAuth2;
using PokerTracker2.Models;
using PokerTracker2.Services;
using System.Linq;

namespace PokerTracker2.Services
{
    /// <summary>
    /// Firebase service using efficient targeted queries and real-time updates.
    /// Downloads minimal data on startup, updates only what changes.
    /// </summary>
    public class FirebaseService
    {
        private FirestoreDb? _firestoreDb;
        private readonly string _credentialsPath;
        private readonly string _projectId;
        private bool _isInitialized = false;
        
        // Debug callback for real-time logging (legacy - will be replaced by LoggingService)
        public static Action<string>? DebugCallback { get; set; }
        
        private void DebugLog(string message)
        {
            // Use unified logging service instead of direct debug output
            LoggingService.Instance.Debug(message, "FirebaseService");
        }

        public FirebaseService(string projectId = "pokertracker-1ece5", string credentialsPath = "")
        {
            _projectId = projectId;
            
            // If no credentials path provided, look for default Firebase credentials
            if (string.IsNullOrEmpty(credentialsPath))
            {
                // Check for GOOGLE_APPLICATION_CREDENTIALS environment variable
                var envCredentials = Environment.GetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS");
                if (!string.IsNullOrEmpty(envCredentials) && File.Exists(envCredentials))
                {
                    _credentialsPath = envCredentials;
                }
                else
                {
                    // Look for service account key in common locations - prioritize project root
                    var commonPaths = new[]
                    {
                        Path.Combine(Directory.GetCurrentDirectory(), "firebase-credentials.json"),
                        Path.Combine(Directory.GetCurrentDirectory(), "..", "firebase-credentials.json"),
                        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "PokerTracker2", "firebase-credentials.json")
                    };

                    foreach (var path in commonPaths)
                    {
                        if (File.Exists(path))
                        {
                            _credentialsPath = path;
                            break;
                        }
                    }
                }
            }
            else
            {
                _credentialsPath = credentialsPath;
            }
        }

        /// <summary>
        /// Initialize the Firebase connection
        /// </summary>
        public async Task<bool> InitializeAsync()
        {
            try
            {
                if (_isInitialized) return true;

                DebugLog($"Starting initialization for project {_projectId}");
                DebugLog($"Credentials path: {_credentialsPath}");

                // Set up credentials
                if (!string.IsNullOrEmpty(_credentialsPath) && File.Exists(_credentialsPath))
                {
                    DebugLog($"Setting GOOGLE_APPLICATION_CREDENTIALS to {_credentialsPath}");
                    // Set environment variable for credentials
                    Environment.SetEnvironmentVariable("GOOGLE_APPLICATION_CREDENTIALS", _credentialsPath);
                }
                else
                {
                    DebugLog($"WARNING - No valid credentials path found!");
                    DebugLog($"_credentialsPath = '{_credentialsPath}'");
                    DebugLog($"File exists = {File.Exists(_credentialsPath ?? "")}");
                }

                // Create Firestore database instance
                DebugLog($"Creating FirestoreDb for project {_projectId}");
                _firestoreDb = await FirestoreDb.CreateAsync(_projectId);
                
                DebugLog($"FirestoreDb created successfully");
                _isInitialized = true;
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"Initialization failed with exception: {ex.GetType().Name}: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Get only player profiles with passwords for login (minimal startup data)
        /// </summary>
        public async Task<List<PlayerProfile>> GetLoginProfilesAsync()
        {
            try
            {
                if (!await InitializeAsync()) return new List<PlayerProfile>();

                var collection = _firestoreDb!.Collection("player_profiles");
                // Only get profiles that have passwords set
                var query = collection.WhereEqualTo("hasPassword", true);
                var snapshot = await query.GetSnapshotAsync();

                var profiles = new List<PlayerProfile>();
                foreach (var document in snapshot.Documents)
                {
                    var data = document.ToDictionary();
                    
                    var profile = new PlayerProfile(data["name"].ToString() ?? "")
                    {
                        Nickname = data["nickname"].ToString() ?? "",
                        Email = data["email"].ToString() ?? "",
                        Phone = data["phone"].ToString() ?? "",
                        Notes = data["notes"].ToString() ?? "",
                        HasPassword = true, // We know this is true from the query
                        PasswordHash = data["passwordHash"].ToString() ?? "",
                        Salt = data["salt"].ToString() ?? "",
                        CreatedDate = data["createdDate"] is Timestamp created ? created.ToDateTime() : DateTime.MinValue,
                        LastPlayedDate = data["lastPlayedDate"] is Timestamp lastPlayed ? lastPlayed.ToDateTime() : DateTime.MinValue,
                        TotalSessionsPlayed = data["totalSessionsPlayed"] is long sessions ? (int)sessions : 0,
                        TotalLifetimeBuyIn = data["totalLifetimeBuyIn"] is double buyIn ? buyIn : 0.0,
                        TotalLifetimeCashOut = data["totalLifetimeCashOut"] is double cashOut ? cashOut : 0.0,
                        IsActive = data["isActive"] is bool active ? active : true
                    };

                    profiles.Add(profile);
                }

                return profiles;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get login profiles from Firebase: {ex.Message}");
                return new List<PlayerProfile>();
            }
        }

        /// <summary>
        /// Get a specific player profile by name (on-demand loading)
        /// </summary>
        public async Task<PlayerProfile?> GetPlayerProfileAsync(string playerName)
        {
            try
            {
                if (!await InitializeAsync()) return null;

                var collection = _firestoreDb!.Collection("player_profiles");
                var document = await collection.Document(playerName.ToLower()).GetSnapshotAsync();

                if (!document.Exists) return null;

                var data = document.ToDictionary();
                
                var profile = new PlayerProfile(data["name"].ToString() ?? "")
                {
                    Nickname = data["nickname"].ToString() ?? "",
                    Email = data["email"].ToString() ?? "",
                    Phone = data["phone"].ToString() ?? "",
                    Notes = data["notes"].ToString() ?? "",
                    HasPassword = data["hasPassword"] is bool hasPwd ? hasPwd : false,
                    PasswordHash = data["passwordHash"].ToString() ?? "",
                    Salt = data["salt"].ToString() ?? "",
                    CreatedDate = data["createdDate"] is Timestamp created ? created.ToDateTime() : DateTime.MinValue,
                    LastPlayedDate = data["lastPlayedDate"] is Timestamp lastPlayed ? lastPlayed.ToDateTime() : DateTime.MinValue,
                    TotalSessionsPlayed = data["totalSessionsPlayed"] is long sessions ? (int)sessions : 0,
                    TotalLifetimeBuyIn = data["totalLifetimeBuyIn"] is double buyIn ? buyIn : 0.0,
                    TotalLifetimeCashOut = data["totalLifetimeCashOut"] is double cashOut ? cashOut : 0.0,
                    IsActive = data["isActive"] is bool active ? active : true
                };

                return profile;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to get player profile from Firebase: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Save a player profile to Firebase immediately
        /// </summary>
        public async Task<bool> SavePlayerProfileAsync(PlayerProfile profile)
        {
            try
            {
                DebugLog($"Attempting to save player profile: {profile.Name}");
                DebugLog($"Profile details: Nickname='{profile.Nickname}', Email='{profile.Email}', HasPassword={profile.HasPassword}");
                
                if (!await InitializeAsync()) 
                {
                    DebugLog($"❌ CRITICAL: Failed to initialize Firebase");
                    DebugLog($"This means the Firebase connection couldn't be established");
                    return false;
                }

                var collection = _firestoreDb!.Collection("player_profiles");
                var document = collection.Document(profile.Name.ToLower());
                
                DebugLog($"Using collection 'player_profiles', document '{profile.Name.ToLower()}'");

                var data = new Dictionary<string, object>
                {
                    { "name", profile.Name },
                    { "nickname", profile.Nickname },
                    { "email", profile.Email },
                    { "phone", profile.Phone },
                    { "notes", profile.Notes },
                    { "hasPassword", profile.HasPassword },
                    { "passwordHash", profile.PasswordHash },
                    { "salt", profile.Salt },
                    { "createdDate", profile.CreatedDate.Kind == DateTimeKind.Utc ? profile.CreatedDate : profile.CreatedDate.ToUniversalTime() },
                    { "lastPlayedDate", profile.LastPlayedDate.Kind == DateTimeKind.Utc ? profile.LastPlayedDate : profile.LastPlayedDate.ToUniversalTime() },
                    { "totalSessionsPlayed", profile.TotalSessionsPlayed },
                    { "totalLifetimeBuyIn", profile.TotalLifetimeBuyIn },
                    { "totalLifetimeCashOut", profile.TotalLifetimeCashOut },
                    { "isActive", profile.IsActive }
                };

                // Add session references and recent sessions for analytics
                if (profile.SessionIds != null && profile.SessionIds.Count > 0)
                {
                    data.Add("sessionIds", profile.SessionIds);
                }

                if (profile.RecentSessions != null && profile.RecentSessions.Count > 0)
                {
                    var recentSessionsData = new List<Dictionary<string, object>>();
                    foreach (var session in profile.RecentSessions)
                    {
                        var sessionData = new Dictionary<string, object>
                        {
                            { "sessionId", session.SessionId },
                            { "sessionName", session.SessionName },
                            { "sessionDate", session.SessionDate.Kind == DateTimeKind.Utc ? session.SessionDate : session.SessionDate.ToUniversalTime() },
                            { "buyIn", session.BuyIn },
                            { "cashOut", session.CashOut },
                            { "duration", session.Duration.Ticks },
                            { "playerCount", session.PlayerCount }
                        };
                        recentSessionsData.Add(sessionData);
                    }
                    data.Add("recentSessions", recentSessionsData);
                }

                DebugLog($"About to call document.SetAsync with {data.Count} fields");
                DebugLog($"Data preview: Name='{data["name"]}', HasPassword={data["hasPassword"]}");
                
                await document.SetAsync(data);
                
                DebugLog($"✅ Successfully saved player profile to Firebase");
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"❌ CRITICAL: Failed to save player profile to Firebase");
                DebugLog($"Exception type: {ex.GetType().Name}");
                DebugLog($"Error message: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                
                // Additional context for common Firebase errors
                if (ex.Message.Contains("permission"))
                {
                    DebugLog($"🔒 This appears to be a Firebase security rules/permission issue");
                    DebugLog($"Check your Firestore security rules for the 'player_profiles' collection");
                }
                else if (ex.Message.Contains("network") || ex.Message.Contains("timeout"))
                {
                    DebugLog($"🌐 This appears to be a network connectivity issue");
                    DebugLog($"Check your internet connection and firewall settings");
                }
                else if (ex.Message.Contains("credentials") || ex.Message.Contains("authentication"))
                {
                    DebugLog($"🔑 This appears to be a credentials/authentication issue");
                    DebugLog($"Check your firebase-credentials.json file and project ID");
                }
                
                return false;
            }
        }

        /// <summary>
        /// Save a session to Firebase immediately with all detailed player and transaction data
        /// </summary>
        public async Task<bool> SaveSessionAsync(Session session)
        {
            try
            {
                if (!await InitializeAsync()) return false;

                var collection = _firestoreDb!.Collection("sessions");
                var document = collection.Document(session.Id);

                // Convert players to detailed data for Firebase
                var playersData = new List<Dictionary<string, object>>();
                if (session.Players != null)
                {
                    foreach (var player in session.Players)
                    {
                        var playerData = new Dictionary<string, object>
                        {
                            { "name", player.Name },
                            { "totalBuyIn", player.TotalBuyIn },
                            { "totalCashOut", player.TotalCashOut },
                            { "finalStack", player.FinalStack.HasValue ? player.FinalStack.Value : 0.0 },
                            { "lastActivityTime", player.LastActivityTime.Kind == DateTimeKind.Utc ? player.LastActivityTime : player.LastActivityTime.ToUniversalTime() }
                        };
                        playersData.Add(playerData);
                    }
                }

                // Convert transactions to detailed data for Firebase
                var transactionsData = new List<Dictionary<string, object>>();
                if (session.Transactions != null)
                {
                    foreach (var transaction in session.Transactions)
                    {
                        var transactionData = new Dictionary<string, object>
                        {
                            { "playerName", transaction.PlayerName },
                            { "amount", transaction.Amount },
                            { "type", transaction.Type.ToString() },
                            { "timestamp", transaction.Timestamp.Kind == DateTimeKind.Utc ? transaction.Timestamp.ToUniversalTime() : transaction.Timestamp.ToUniversalTime() }
                        };
                        transactionsData.Add(transactionData);
                    }
                }

                var data = new Dictionary<string, object>
                {
                    { "id", session.Id },
                    { "name", session.Name },
                    { "startTime", session.StartTime.Kind == DateTimeKind.Utc ? session.StartTime : session.StartTime.ToUniversalTime() },
                    { "notes", session.Notes ?? "" },
                    { "totalBuyIns", session.TotalBuyIns },
                    { "totalCashOuts", session.TotalCashOuts },
                    { "createdAt", session.CreatedAt.Kind == DateTimeKind.Utc ? session.CreatedAt : session.CreatedAt.ToUniversalTime() },
                    { "updatedAt", session.UpdatedAt.Kind == DateTimeKind.Utc ? session.UpdatedAt : session.UpdatedAt.ToUniversalTime() },
                    { "players", playersData },
                    { "transactions", transactionsData }
                };

                // Only include endTime if the session is actually completed
                if (session.EndTime != DateTime.MinValue)
                {
                    data.Add("endTime", session.EndTime.Kind == DateTimeKind.Utc ? session.EndTime : session.EndTime.ToUniversalTime());
                }

                await document.SetAsync(data);
                DebugLog($"Saved session to Firebase with {playersData.Count} players and {transactionsData.Count} transactions");

                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to save session to Firebase: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a player profile from Firebase
        /// </summary>
        public async Task<bool> DeletePlayerProfileAsync(string playerName)
        {
            try
            {
                if (!await InitializeAsync()) return false;

                var collection = _firestoreDb!.Collection("player_profiles");
                await collection.Document(playerName.ToLower()).DeleteAsync();
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to delete player profile from Firebase: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Delete a session from Firebase
        /// </summary>
        public async Task<bool> DeleteSessionAsync(string sessionId)
        {
            try
            {
                if (!await InitializeAsync()) return false;

                var collection = _firestoreDb!.Collection("sessions");
                await collection.Document(sessionId).DeleteAsync();
                DebugLog($"Successfully deleted session {sessionId} from Firebase");
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to delete session from Firebase: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Get all player profiles from Firebase (for admin/management purposes)
        /// </summary>
        public async Task<List<PlayerProfile>> GetAllPlayerProfilesAsync()
        {
            try
            {
                if (!await InitializeAsync()) return new List<PlayerProfile>();

                var collection = _firestoreDb!.Collection("player_profiles");
                var snapshot = await collection.GetSnapshotAsync();

                var profiles = new List<PlayerProfile>();
                foreach (var document in snapshot.Documents)
                {
                    var data = document.ToDictionary();
                    
                    var profile = new PlayerProfile(data["name"].ToString() ?? "")
                    {
                        Nickname = data["nickname"].ToString() ?? "",
                        Email = data["email"].ToString() ?? "",
                        Phone = data["phone"].ToString() ?? "",
                        Notes = data["notes"].ToString() ?? "",
                        HasPassword = data["hasPassword"] is bool hasPwd ? hasPwd : false,
                        PasswordHash = data["passwordHash"].ToString() ?? "",
                        Salt = data["salt"].ToString() ?? "",
                        CreatedDate = data["createdDate"] is Timestamp created ? created.ToDateTime() : DateTime.MinValue,
                        LastPlayedDate = data["lastPlayedDate"] is Timestamp lastPlayed ? lastPlayed.ToDateTime() : DateTime.MinValue,
                        TotalSessionsPlayed = data["totalSessionsPlayed"] is long sessions ? (int)sessions : 0,
                        TotalLifetimeBuyIn = data["totalLifetimeBuyIn"] is double buyIn ? buyIn : 0.0,
                        TotalLifetimeCashOut = data["totalLifetimeCashOut"] is double cashOut ? cashOut : 0.0,
                        IsActive = data["isActive"] is bool active ? active : true
                    };

                    // Load session references and recent sessions if they exist
                    if (data.ContainsKey("sessionIds") && data["sessionIds"] is List<object> sessionIdsList)
                    {
                        profile.SessionIds = sessionIdsList.Select(id => id.ToString() ?? "").ToList();
                    }

                    if (data.ContainsKey("recentSessions") && data["recentSessions"] is List<object> recentSessionsList)
                    {
                        var recentSessions = new List<PlayerSessionSummary>();
                        foreach (var sessionObj in recentSessionsList)
                        {
                            if (sessionObj is Dictionary<string, object> sessionData)
                            {
                                var sessionSummary = new PlayerSessionSummary(
                                    sessionData["sessionId"].ToString() ?? "",
                                    sessionData["sessionName"].ToString() ?? "",
                                    sessionData["sessionDate"] is Timestamp date ? date.ToDateTime() : DateTime.MinValue,
                                    sessionData["buyIn"] is double buyInAmount ? buyInAmount : 0.0,
                                    sessionData["cashOut"] is double cashOutAmount ? cashOutAmount : 0.0,
                                    sessionData["duration"] is long duration ? TimeSpan.FromTicks(duration) : TimeSpan.Zero,
                                    sessionData["playerCount"] is long count ? (int)count : 0
                                );
                                recentSessions.Add(sessionSummary);
                            }
                        }
                        profile.RecentSessions = recentSessions;
                    }

                    profiles.Add(profile);
                }

                return profiles;
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to get all player profiles from Firebase: {ex.Message}");
                return new List<PlayerProfile>();
            }
        }

        /// <summary>
        /// Get recent sessions from Firebase with full player and transaction details
        /// </summary>
        public async Task<List<Session>> GetRecentSessionsAsync(int daysBack = 30)
        {
            try
            {
                if (!await InitializeAsync()) return new List<Session>();

                var collection = _firestoreDb!.Collection("sessions");
                var cutoffDate = DateTime.UtcNow.AddDays(-daysBack);
                
                var snapshot = await collection
                    .WhereGreaterThan("startTime", cutoffDate)
                    .OrderByDescending("startTime")
                    .GetSnapshotAsync();

                var sessions = new List<Session>();
                foreach (var document in snapshot.Documents)
                {
                    var data = document.ToDictionary();
                    
                    var session = new Session
                    {
                        Id = data["id"].ToString() ?? "",
                        Name = data["name"].ToString() ?? "",
                        StartTime = data["startTime"] is Timestamp start ? start.ToDateTime() : DateTime.MinValue,
                        EndTime = data.ContainsKey("endTime") && data["endTime"] is Timestamp end ? end.ToDateTime() : DateTime.MinValue,
                        Notes = data["notes"].ToString() ?? "",
                        TotalBuyIns = data["totalBuyIns"] is double buyIns ? buyIns : 0.0,
                        TotalCashOuts = data["totalCashOuts"] is double cashOuts ? cashOuts : 0.0,
                        // REMOVED: NetProfit field - poker sessions should always balance to 0
                        // NetProfit = data["netProfit"] is double profit ? profit : 0.0,
                        CreatedAt = data["createdAt"] is Timestamp created ? created.ToDateTime() : DateTime.MinValue,
                        UpdatedAt = data["updatedAt"] is Timestamp updated ? updated.ToDateTime() : DateTime.MinValue
                    };

                    // Reconstruct players
                    if (data.ContainsKey("players") && data["players"] is List<object> playersList)
                    {
                        var players = new List<Player>();
                        foreach (var playerObj in playersList)
                        {
                            if (playerObj is Dictionary<string, object> playerData)
                            {
                                var player = new Player(
                                    playerData["name"].ToString() ?? "",
                                    playerData["totalBuyIn"] is double buyIn ? buyIn : 0.0
                                )
                                {
                                    TotalCashOut = playerData["totalCashOut"] is double cashOut ? cashOut : 0.0,
                                    FinalStack = playerData["finalStack"] is double stack ? stack : null,
                                    LastActivityTime = playerData["lastActivityTime"] is Timestamp last ? last.ToDateTime() : DateTime.MinValue
                                };
                                players.Add(player);
                            }
                        }
                        session.Players = players;
                    }

                    // Reconstruct transactions
                    if (data.ContainsKey("transactions") && data["transactions"] is List<object> transactionsList)
                    {
                        var transactions = new List<PokerTracker2.Models.Transaction>();
                        foreach (var transactionObj in transactionsList)
                        {
                            if (transactionObj is Dictionary<string, object> transactionData)
                            {
                                var transactionType = Enum.TryParse<TransactionType>(
                                    transactionData["type"].ToString() ?? "", 
                                    out var type) ? type : TransactionType.BuyIn;
                                
                                var transaction = new PokerTracker2.Models.Transaction(
                                    transactionData["playerName"].ToString() ?? "",
                                    transactionData["amount"] is double amount ? amount : 0.0,
                                    transactionType
                                )
                                {
                                    Timestamp = transactionData["timestamp"] is Timestamp timestamp ? timestamp.ToDateTime() : DateTime.MinValue
                                };
                                transactions.Add(transaction);
                            }
                        }
                        session.Transactions = transactions;
                    }

                    sessions.Add(session);
                }

                DebugLog($"Retrieved {sessions.Count} sessions from Firebase");
                return sessions;
            }
            catch (Exception ex)
            {
                DebugLog($"Failed to get recent sessions from Firebase: {ex.Message}");
                return new List<Session>();
            }
        }

        /// <summary>
        /// Test the Firebase connection
        /// </summary>
        public async Task<bool> TestConnectionAsync()
        {
            try
            {
                DebugLog($"Testing Firebase connection...");
                
                if (!await InitializeAsync()) 
                {
                    DebugLog($"Failed to initialize for connection test");
                    return false;
                }

                // Try to access a collection to test the connection
                var collection = _firestoreDb!.Collection("test");
                DebugLog($"Testing access to 'test' collection");
                
                var snapshot = await collection.GetSnapshotAsync();
                DebugLog($"Successfully accessed 'test' collection, found {snapshot.Documents.Count} documents");
                
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"Connection test failed: {ex.GetType().Name}: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }

        /// <summary>
        /// Create a test document to verify write permissions
        /// </summary>
        public async Task<bool> TestWriteAsync()
        {
            try
            {
                DebugLog($"Testing Firebase write permissions...");
                
                if (!await InitializeAsync()) 
                {
                    DebugLog($"Failed to initialize for write test");
                    return false;
                }

                // Try to create a test document
                var collection = _firestoreDb!.Collection("test");
                var testDoc = collection.Document("connection_test");
                
                var testData = new Dictionary<string, object>
                {
                    { "timestamp", DateTime.UtcNow },
                    { "message", "Connection test successful" },
                    { "app", "PokerTracker2" }
                };

                DebugLog($"Creating test document 'connection_test'");
                await testDoc.SetAsync(testData);
                
                DebugLog($"Successfully created test document");
                
                // Clean up the test document
                await testDoc.DeleteAsync();
                DebugLog($"Clean up test document");
                
                return true;
            }
            catch (Exception ex)
            {
                DebugLog($"Write test failed: {ex.GetType().Name}: {ex.Message}");
                DebugLog($"Stack trace: {ex.StackTrace}");
                return false;
            }
        }
    }
}
