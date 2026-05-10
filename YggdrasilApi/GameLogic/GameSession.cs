using System;
using System.Collections.Generic;
using System.Linq;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

/// <summary>
/// Manages a game session including all players, units, and user input requests.
/// See GameSession.Players.cs and GameSession.Units.cs for the management methods.
/// </summary>
public partial class GameSession(string id)
{
    public string Id { get; set; } = id ?? throw new ArgumentNullException(nameof(id));
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public Dictionary<string, Player> Players { get; set; } = new();
    public Dictionary<int, Unit> Units { get; set; } = new();
    public Dictionary<string, UserInputRequest> PendingInputRequests { get; set; } = new();
    public Dictionary<string, UserInputRequest> ResolvedInputRequests { get; set; } = new();
    public Dictionary<string, Template> Templates { get; set; } = new();
    public Dictionary<string, SessionUser> Users { get; set; } = new();

    /// <summary>How long the session has been running.</summary>
    public TimeSpan Duration => DateTime.UtcNow - CreatedAt;

    /// <summary>Number of players currently in the session.</summary>
    public int ActivePlayerCount => Players.Count;

    /// <summary>Total number of units registered in the session.</summary>
    public int TotalUnitCount => Units.Count;

    /// <summary>
    /// Adds a user to the session with the specified role.
    /// </summary>
    public void AddUser(SessionUser user)
    {
        if (user == null) throw new ArgumentNullException(nameof(user));
        Users[user.UserId] = user;
    }

    /// <summary>
    /// Gets a user by ID.
    /// </summary>
    public SessionUser? GetUser(string userId)
    {
        Users.TryGetValue(userId, out var user);
        return user;
    }

    /// <summary>
    /// Removes a user from the session.
    /// </summary>
    public bool RemoveUser(string userId)
    {
        return Users.Remove(userId);
    }

    /// <summary>
    /// Updates a user's role.
    /// </summary>
    public bool UpdateUserRole(string userId, SessionUserRole newRole)
    {
        if (!Users.TryGetValue(userId, out var user))
            return false;

        user.Role = newRole;
        return true;
    }

    /// <summary>
    /// Gets all users with a specific role.
    /// </summary>
    public List<SessionUser> GetUsersByRole(SessionUserRole role)
    {
        return Users.Values.Where(u => u.Role == role).ToList();
    }

    /// <summary>
    /// Gets all pending users (role == Pending).
    /// </summary>
    public List<SessionUser> GetPendingUsers()
    {
        return GetUsersByRole(SessionUserRole.Pending);
    }

    /// <summary>
    /// Gets all approved users (Player, CoLeader, or SessionLeader).
    /// </summary>
    public List<SessionUser> GetApprovedUsers()
    {
        return Users.Values
            .Where(u => u.Role is SessionUserRole.Player or SessionUserRole.CoLeader or SessionUserRole.SessionLeader)
            .ToList();
    }

    /// <summary>
    /// Gets all connected users.
    /// </summary>
    public List<SessionUser> GetConnectedUsers()
    {
        return Users.Values.Where(u => u.IsConnected).ToList();
    }

    /// <summary>
    /// Gets all authorized users (can make decisions in the session).
    /// </summary>
    public List<SessionUser> GetAuthorizedUsers()
    {
        return Users.Values
            .Where(u => u.Role is SessionUserRole.CoLeader or SessionUserRole.SessionLeader)
            .ToList();
    }

    /// <summary>
    /// Gets the session leader.
    /// </summary>
    public SessionUser? GetSessionLeader()
    {
        return Users.Values.FirstOrDefault(u => u.Role == SessionUserRole.SessionLeader);
    }

    /// <summary>
    /// Checks if a user has permission to manage roles.
    /// </summary>
    public bool CanManageRoles(string userId)
    {
        var user = GetUser(userId);
        return user?.Role is SessionUserRole.SessionLeader or SessionUserRole.CoLeader;
    }

    /// <summary>
    /// Checks if a user is blacklisted.
    /// </summary>
    public bool IsBlackListed(string userId)
    {
        var user = GetUser(userId);
        return user?.Role == SessionUserRole.BlackListed;
    }
}
