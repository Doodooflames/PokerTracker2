using System;
using System.Collections.Generic;
using System.Linq;
using PokerTracker2.Models;
using PokerTracker2.Services;

namespace PokerTracker2.Services
{
    /// <summary>
    /// Service for handling permission checks and access control throughout the application
    /// </summary>
    public class PermissionService
    {
        private readonly User _currentUser;
        private readonly SessionManager _sessionManager;
        private readonly PlayerManager _playerManager;

        public PermissionService(User currentUser, SessionManager sessionManager, PlayerManager playerManager)
        {
            _currentUser = currentUser ?? throw new ArgumentNullException(nameof(currentUser));
            _sessionManager = sessionManager ?? throw new ArgumentNullException(nameof(sessionManager));
            _playerManager = playerManager ?? throw new ArgumentNullException(nameof(playerManager));
        }

        #region Session Permissions

        /// <summary>
        /// Check if user can view a specific session
        /// </summary>
        public bool CanViewSession(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only view sessions they hosted or participated in
            return IsSessionHost(session) || IsSessionParticipant(session);
        }

        /// <summary>
        /// Check if user can edit a specific session
        /// </summary>
        public bool CanEditSession(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only edit sessions they hosted
            if (!IsSessionHost(session)) return false;

            // Non-admins cannot edit completed sessions
            if (session.IsCompleted) return false;

            return true;
        }

        /// <summary>
        /// Check if user can delete a specific session
        /// </summary>
        public bool CanDeleteSession(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only delete sessions they hosted
            return IsSessionHost(session);
        }

        /// <summary>
        /// Check if user can end a specific session
        /// </summary>
        public bool CanEndSession(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only end sessions they hosted
            return IsSessionHost(session);
        }

        /// <summary>
        /// Check if user can add players to a specific session
        /// </summary>
        public bool CanAddPlayersToSession(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only add players to sessions they hosted
            if (!IsSessionHost(session)) return false;

            // Non-admins cannot add players to completed sessions
            if (session.IsCompleted) return false;

            return true;
        }

        /// <summary>
        /// Check if user can modify session data (buy-ins, cash-outs, etc.)
        /// </summary>
        public bool CanModifySessionData(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only modify sessions they hosted
            if (!IsSessionHost(session)) return false;

            // Non-admins cannot modify completed sessions
            if (session.IsCompleted) return false;

            return true;
        }

        #endregion

        #region Player Profile Permissions

        /// <summary>
        /// Check if user can create new player profiles
        /// </summary>
        public bool CanCreatePlayerProfiles()
        {
            // All authenticated users can create player profiles
            return _currentUser != null;
        }

        /// <summary>
        /// Check if user can set passwords for player profiles
        /// </summary>
        public bool CanSetPlayerPasswords()
        {
            // Only admins can set passwords for player profiles
            return _currentUser.IsAdmin;
        }

        /// <summary>
        /// Check if user can edit a specific player profile
        /// </summary>
        public bool CanEditPlayerProfile(PlayerProfile profile)
        {
            if (profile == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only edit their own profile
            return profile.Name.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user can delete a specific player profile
        /// </summary>
        public bool CanDeletePlayerProfile(PlayerProfile profile)
        {
            if (profile == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins cannot delete any profiles
            return false;
        }

        #endregion

        #region Analytics and Reports Permissions

        /// <summary>
        /// Check if user can view global analytics
        /// </summary>
        public bool CanViewGlobalAnalytics()
        {
            // Only admins can view global analytics
            return _currentUser.IsAdmin;
        }

        /// <summary>
        /// Check if user can view other players' detailed statistics
        /// </summary>
        public bool CanViewOtherPlayerStats(PlayerProfile profile)
        {
            if (profile == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only view their own stats
            return profile.Name.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Check if user can export session data
        /// </summary>
        public bool CanExportSessionData(Session session)
        {
            if (session == null) return false;
            if (_currentUser.IsAdmin) return true;

            // Non-admins can only export sessions they participated in
            return IsSessionHost(session) || IsSessionParticipant(session);
        }

        #endregion

        #region System Administration Permissions

        /// <summary>
        /// Check if user can manage other users
        /// </summary>
        public bool CanManageUsers()
        {
            return _currentUser.IsAdmin;
        }

        /// <summary>
        /// Check if user can view system logs
        /// </summary>
        public bool CanViewSystemLogs()
        {
            return _currentUser.IsAdmin;
        }

        /// <summary>
        /// Check if user can access debug features
        /// </summary>
        public bool CanAccessDebugFeatures()
        {
            return _currentUser.IsAdmin;
        }

        /// <summary>
        /// Check if user can manage admin status of other players
        /// </summary>
        public bool CanManageAdminStatus()
        {
            return _currentUser.IsAdmin;
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Check if current user is the host of a session
        /// </summary>
        private bool IsSessionHost(Session session)
        {
            // Check if user is the explicit host or creator of the session
            return session.HostedBy?.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase) == true ||
                   session.CreatedBy?.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase) == true;
        }

        /// <summary>
        /// Check if current user participated in a session
        /// </summary>
        private bool IsSessionParticipant(Session session)
        {
            if (session.Players == null) return false;
            return session.Players.Any(p => p.Name.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase));
        }

        #endregion

        #region Filtered Data Access

        /// <summary>
        /// Get sessions that the current user can view
        /// </summary>
        public List<Session> GetViewableSessions(IEnumerable<Session> allSessions)
        {
            if (_currentUser.IsAdmin) return allSessions.ToList();

            return allSessions.Where(s => CanViewSession(s)).ToList();
        }

        /// <summary>
        /// Get sessions that the current user can edit
        /// </summary>
        public List<Session> GetEditableSessions(IEnumerable<Session> allSessions)
        {
            if (_currentUser.IsAdmin) return allSessions.ToList();

            return allSessions.Where(s => CanEditSession(s)).ToList();
        }

        /// <summary>
        /// Get player profiles that the current user can view
        /// </summary>
        public List<PlayerProfile> GetViewablePlayerProfiles(IEnumerable<PlayerProfile> allProfiles)
        {
            if (_currentUser.IsAdmin) return allProfiles.ToList();

            // Non-admins can only view their own profile
            return allProfiles.Where(p => p.Name.Equals(_currentUser.Username, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        #endregion

        #region Permission Checking Methods for UI

        /// <summary>
        /// Check if a specific action is allowed and return a user-friendly message
        /// </summary>
        public (bool IsAllowed, string Message) CheckPermission(string action, object target = null)
        {
            switch (action.ToLower())
            {
                case "create_session":
                    return (true, "Permission granted");

                case "edit_session":
                    if (target is Session session)
                    {
                        var canEdit = CanEditSession(session);
                        return (canEdit, canEdit ? "Permission granted" : "You can only edit sessions you hosted");
                    }
                    return (false, "Invalid target for permission check");

                case "end_session":
                    if (target is Session endSession)
                    {
                        var canEnd = CanEndSession(endSession);
                        return (canEnd, canEnd ? "Permission granted" : "You can only end sessions you hosted");
                    }
                    return (false, "Invalid target for permission check");

                case "add_players":
                    if (target is Session addSession)
                    {
                        var canAdd = CanAddPlayersToSession(addSession);
                        return (canAdd, canAdd ? "Permission granted" : "You can only add players to sessions you hosted");
                    }
                    return (false, "Invalid target for permission check");

                case "modify_session_data":
                    if (target is Session modSession)
                    {
                        var canModify = CanModifySessionData(modSession);
                        if (!canModify)
                        {
                            if (modSession.IsCompleted)
                                return (false, "Cannot modify completed sessions");
                            return (false, "You can only modify sessions you hosted");
                        }
                        return (true, "Permission granted");
                    }
                    return (false, "Invalid target for permission check");

                case "set_player_password":
                    var canSetPassword = CanSetPlayerPasswords();
                    return (canSetPassword, canSetPassword ? "Permission granted" : "Only administrators can set player passwords");

                case "edit_player_profile":
                    if (target is PlayerProfile profile)
                    {
                        var canEditProfile = CanEditPlayerProfile(profile);
                        return (canEditProfile, canEditProfile ? "Permission granted" : "You can only edit your own profile");
                    }
                    return (false, "Invalid target for permission check");

                case "view_global_analytics":
                    var canViewAnalytics = CanViewGlobalAnalytics();
                    return (canViewAnalytics, canViewAnalytics ? "Permission granted" : "Only administrators can view global analytics");

                case "manage_admin_status":
                    var canManageAdmin = CanManageAdminStatus();
                    return (canManageAdmin, canManageAdmin ? "Permission granted" : "Only administrators can manage admin status");

                default:
                    return (false, "Unknown permission action");
            }
        }

        #endregion
    }
}
