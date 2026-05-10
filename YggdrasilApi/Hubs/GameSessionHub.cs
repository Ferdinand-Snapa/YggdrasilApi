using Microsoft.AspNetCore.SignalR;
using YggdrasilApi.GameLogick;
using YggdrasilApi.Services;

namespace YggdrasilApi.Hubs;

/// <summary>
/// SignalR Hub for real-time game session communication with role-based access control.
/// </summary>
public class GameSessionHub : Hub
{
    private readonly ISessionService _sessionService;
    private readonly ILogger<GameSessionHub> _logger;

    public GameSessionHub(ISessionService sessionService, ILogger<GameSessionHub> logger)
    {
        _sessionService = sessionService;
        _logger = logger;
    }

    /// <summary>
    /// Called when a client connects.
    /// Query params: sessionId and userId
    /// Example: ws://localhost:5000/gamesession?sessionId=game-001&userId=user-123
    /// </summary>
    public override async Task OnConnectedAsync()
    {
        var sessionId = Context.GetHttpContext()?.Request.Query["sessionId"].ToString();
        var userId = Context.GetHttpContext()?.Request.Query["userId"].ToString();

        if (string.IsNullOrEmpty(sessionId) || string.IsNullOrEmpty(userId))
        {
            _logger.LogWarning("Connection rejected: missing sessionId or userId");
            Context.Abort();
            return;
        }

        // Verify session exists
        var session = _sessionService.GetSession(sessionId);
        if (session == null)
        {
            _logger.LogWarning($"Connection rejected: session '{sessionId}' not found");
            Context.Abort();
            return;
        }

        // Get or create user
        var user = _sessionService.GetUser(sessionId, userId);

        // If user doesn't exist and session is full or has restrictions, reject
        if (user == null)
        {
            _logger.LogWarning($"Connection rejected: user '{userId}' not found in session '{sessionId}'");
            Context.Abort();
            return;
        }

        // Check if user is blacklisted
        if (_sessionService.IsUserBlackListed(sessionId, userId))
        {
            _logger.LogWarning($"Connection rejected: user '{userId}' is blacklisted from session '{sessionId}'");
            Context.Abort();
            return;
        }

        // Store session/user info in connection items
        Context.Items["SessionId"] = sessionId;
        Context.Items["UserId"] = userId;

        // Update user connection status
        user.ConnectionId = Context.ConnectionId;
        user.IsConnected = true;

        // Add connection to groups for targeted messaging
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}");
        await Groups.AddToGroupAsync(Context.ConnectionId, $"user-{userId}");

        // Add to role-specific groups
        await Groups.AddToGroupAsync(Context.ConnectionId, $"session-{sessionId}-{user.Role}");

        _logger.LogInformation($"User {userId} ({user.UserName}) connected to session {sessionId} with role {user.Role}");

        // Send connection confirmation to the user
        await Clients.Caller.SendAsync("ConnectionEstablished", new
        {
            SessionId = sessionId,
            UserId = userId,
            Role = user.Role.ToString(),
            Timestamp = DateTime.UtcNow
        });

        // Notify authorized users about the new connection
        var authorizedUsers = _sessionService.GetAuthorizedUsers(sessionId);
        await Clients.Group($"session-{sessionId}-SessionLeader")
            .SendAsync("UserConnected", new
            {
                UserId = userId,
                UserName = user.UserName,
                Role = user.Role.ToString(),
                Timestamp = DateTime.UtcNow
            });

        await Clients.Group($"session-{sessionId}-CoLeader")
            .SendAsync("UserConnected", new
            {
                UserId = userId,
                UserName = user.UserName,
                Role = user.Role.ToString(),
                Timestamp = DateTime.UtcNow
            });

        // If user is pending, notify authorized users for role assignment
        if (user.Role == SessionUserRole.Pending)
        {
            await NotifyPendingUserApproval(sessionId, userId, user.UserName);
        }

        await base.OnConnectedAsync();
    }

    /// <summary>
    /// Called when a client disconnects.
    /// </summary>
    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        if (Context.Items.TryGetValue("SessionId", out var sessionIdObj) &&
            Context.Items.TryGetValue("UserId", out var userIdObj))
        {
            var sessionId = sessionIdObj?.ToString();
            var userId = userIdObj?.ToString();

            if (sessionId != null && userId != null)
            {
                var user = _sessionService.GetUser(sessionId, userId);
                if (user != null)
                {
                    user.IsConnected = false;
                    _logger.LogInformation($"User {userId} disconnected from session {sessionId}");

                    // Notify other clients
                    await Clients.Group($"session-{sessionId}").SendAsync("UserDisconnected", new
                    {
                        UserId = userId,
                        UserName = user.UserName,
                        Timestamp = DateTime.UtcNow
                    });
                }
            }
        }

        await base.OnDisconnectedAsync(exception);
    }

    /// <summary>
    /// Allows authorized users to assign a role to a pending user.
    /// </summary>
    public async Task AssignRoleToUser(string sessionId, string pendingUserId, string roleName)
    {
        if (!Context.Items.TryGetValue("UserId", out var authorizedUserIdObj))
        {
            await Clients.Caller.SendAsync("RoleAssignmentError", new { error = "Invalid connection context" });
            return;
        }

        var authorizedUserId = authorizedUserIdObj?.ToString();
        if (authorizedUserId == null)
        {
            await Clients.Caller.SendAsync("RoleAssignmentError", new { error = "Invalid connection context" });
            return;
        }

        try
        {
            // Check if caller has permission to assign roles
            if (!_sessionService.CanManageRoles(sessionId, authorizedUserId))
            {
                _logger.LogWarning($"User {authorizedUserId} attempted to assign role without permission");
                await Clients.Caller.SendAsync("RoleAssignmentError", new { error = "You don't have permission to assign roles" });
                return;
            }

            // Parse the role
            if (!Enum.TryParse<SessionUserRole>(roleName, true, out var newRole))
            {
                await Clients.Caller.SendAsync("RoleAssignmentError", new { error = $"Invalid role: {roleName}" });
                return;
            }

            // Cannot assign SessionLeader role
            if (newRole == SessionUserRole.SessionLeader)
            {
                await Clients.Caller.SendAsync("RoleAssignmentError", new { error = "Cannot assign SessionLeader role" });
                return;
            }

            var pendingUser = _sessionService.GetUser(sessionId, pendingUserId);
            if (pendingUser == null)
            {
                await Clients.Caller.SendAsync("RoleAssignmentError", new { error = "User not found" });
                return;
            }

            // Update the user role
            _sessionService.UpdateUserRole(sessionId, pendingUserId, newRole);

            _logger.LogInformation($"User {authorizedUserId} assigned role {newRole} to user {pendingUserId}");

            // Notify the pending user about their role assignment
            await Clients.Group($"user-{pendingUserId}").SendAsync("RoleAssigned", new
            {
                NewRole = newRole.ToString(),
                AssignedBy = _sessionService.GetUser(sessionId, authorizedUserId)?.UserName,
                Timestamp = DateTime.UtcNow
            });

            // Notify all users in session about the role change
            await Clients.Group($"session-{sessionId}").SendAsync("UserRoleUpdated", new
            {
                UserId = pendingUserId,
                UserName = pendingUser.UserName,
                NewRole = newRole.ToString(),
                Timestamp = DateTime.UtcNow
            });

            // Acknowledge to the caller
            await Clients.Caller.SendAsync("RoleAssignmentSuccess", new
            {
                UserId = pendingUserId,
                NewRole = newRole.ToString(),
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error assigning role: {ex.Message}");
            await Clients.Caller.SendAsync("RoleAssignmentError", new { error = ex.Message });
        }
    }

    /// <summary>
    /// Allows authorized users to remove/kick a user from the session.
    /// </summary>
    public async Task RemoveUserFromSession(string sessionId, string userIdToRemove)
    {
        if (!Context.Items.TryGetValue("UserId", out var authorizedUserIdObj))
        {
            await Clients.Caller.SendAsync("RemoveUserError", new { error = "Invalid connection context" });
            return;
        }

        var authorizedUserId = authorizedUserIdObj?.ToString();
        if (authorizedUserId == null)
        {
            await Clients.Caller.SendAsync("RemoveUserError", new { error = "Invalid connection context" });
            return;
        }

        try
        {
            // Check if caller has permission
            if (!_sessionService.CanManageRoles(sessionId, authorizedUserId))
            {
                await Clients.Caller.SendAsync("RemoveUserError", new { error = "You don't have permission to remove users" });
                return;
            }

            // Cannot remove session leader
            var userToRemove = _sessionService.GetUser(sessionId, userIdToRemove);
            if (userToRemove?.Role == SessionUserRole.SessionLeader)
            {
                await Clients.Caller.SendAsync("RemoveUserError", new { error = "Cannot remove session leader" });
                return;
            }

            _sessionService.RemoveUserFromSession(sessionId, userIdToRemove);

            _logger.LogInformation($"User {authorizedUserId} removed user {userIdToRemove} from session {sessionId}");

            // Notify the removed user
            await Clients.Group($"user-{userIdToRemove}").SendAsync("RemovedFromSession", new
            {
                Reason = "You have been removed from the session",
                Timestamp = DateTime.UtcNow
            });

            // Notify all other users
            await Clients.Group($"session-{sessionId}").SendAsync("UserRemoved", new
            {
                UserId = userIdToRemove,
                RemovedBy = _sessionService.GetUser(sessionId, authorizedUserId)?.UserName,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error removing user: {ex.Message}");
            await Clients.Caller.SendAsync("RemoveUserError", new { error = ex.Message });
        }
    }

    /// <summary>
    /// Broadcasts input request to a unit/user.
    /// </summary>
    public async Task SendInputRequest(string sessionId, string userId, UserInputRequest inputRequest)
    {
        if (!Context.Items.TryGetValue("UserId", out var senderIdObj) ||
            !Context.Items.TryGetValue("SessionId", out var senderSessionObj))
        {
            await Clients.Caller.SendAsync("InputRequestError", new { error = "Invalid connection context" });
            return;
        }

        var senderId = senderIdObj?.ToString();
        if (senderId == null || !_sessionService.CanManageRoles(sessionId, senderId))
        {
            await Clients.Caller.SendAsync("InputRequestError", new { error = "You don't have permission to send requests" });
            return;
        }

        try
        {
            await Clients.Group($"user-{userId}").SendAsync("InputRequest", new
            {
                RequestId = inputRequest.Id,
                Schema = inputRequest.Schema,
                Timestamp = inputRequest.CreatedAt
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error sending input request: {ex.Message}");
            await Clients.Caller.SendAsync("InputRequestError", new { error = ex.Message });
        }
    }

    /// <summary>
    /// Heartbeat/ping to keep connection alive.
    /// </summary>
    public async Task Ping()
    {
        await Clients.Caller.SendAsync("Pong", new { Timestamp = DateTime.UtcNow });
    }

    /// <summary>
    /// Get current session users (authorized users only).
    /// </summary>
    public async Task GetSessionUsers()
    {
        if (!Context.Items.TryGetValue("SessionId", out var sessionIdObj) ||
            !Context.Items.TryGetValue("UserId", out var userIdObj))
        {
            await Clients.Caller.SendAsync("SessionUsersError", new { error = "Invalid connection context" });
            return;
        }

        var sessionId = sessionIdObj?.ToString();
        if (sessionId == null)
        {
            await Clients.Caller.SendAsync("SessionUsersError", new { error = "Invalid session" });
            return;
        }

        try
        {
            var users = _sessionService.GetSessionUsers(sessionId);
            var userDtos = users.Select(u => new
            {
                u.UserId,
                u.UserName,
                Role = u.Role.ToString(),
                u.IsConnected,
                u.JoinedAt
            }).ToList();

            await Clients.Caller.SendAsync("SessionUsersList", new
            {
                Users = userDtos,
                Timestamp = DateTime.UtcNow
            });
        }
        catch (Exception ex)
        {
            _logger.LogError($"Error getting session users: {ex.Message}");
            await Clients.Caller.SendAsync("SessionUsersError", new { error = ex.Message });
        }
    }

    /// <summary>
    /// Notifies authorized users that a new user is pending approval.
    /// </summary>
    private async Task NotifyPendingUserApproval(string sessionId, string pendingUserId, string pendingUserName)
    {
        await Clients.Group($"session-{sessionId}-SessionLeader")
            .SendAsync("PendingUserApproval", new
            {
                UserId = pendingUserId,
                UserName = pendingUserName,
                AvailableRoles = new[] { "Spectator", "Player", "CoLeader", "BlackListed" },
                Timestamp = DateTime.UtcNow
            });

        await Clients.Group($"session-{sessionId}-CoLeader")
            .SendAsync("PendingUserApproval", new
            {
                UserId = pendingUserId,
                UserName = pendingUserName,
                AvailableRoles = new[] { "Spectator", "Player", "CoLeader", "BlackListed" },
                Timestamp = DateTime.UtcNow
            });
    }
}
