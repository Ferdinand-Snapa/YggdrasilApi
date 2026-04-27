using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

/// <summary>
/// Manages a game session including all players, units, and user input requests.
/// </summary>
public class GameSession
{
    public string Id { get; set; }
    public DateTime CreatedAt { get; set; }
    public Dictionary<string, Player> Players { get; set; } = new Dictionary<string, Player>();
    public Dictionary<int, Unit> Units { get; set; } = new Dictionary<int, Unit>();
    public Dictionary<string, UserInputRequest> PendingInputRequests { get; set; } = new Dictionary<string, UserInputRequest>();
    public Dictionary<string, UserInputRequest> ResolvedInputRequests { get; set; } = new Dictionary<string, UserInputRequest>();
    public Dictionary<string, Template> Templates { get; set; } = new Dictionary<string, Template>();

    public GameSession(string id)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Adds a player to the session.
    /// </summary>
    public void AddPlayer(Player player)
    {
        if (player == null) throw new ArgumentNullException(nameof(player));
        Players[player.Id] = player;
    }

    /// <summary>
    /// Removes a player from the session.
    /// </summary>
    public bool RemovePlayer(string playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
            return false;

        // Remove all units associated with this player
        foreach (var unitId in player.UnitIds.ToList())
        {
            Units.Remove(unitId);
        }

        return Players.Remove(playerId);
    }

    /// <summary>
    /// Gets a player by ID.
    /// </summary>
    public Player? GetPlayer(string playerId)
    {
        Players.TryGetValue(playerId, out var player);
        return player;
    }

    /// <summary>
    /// Assigns a unit to a player.
    /// </summary>
    public bool AssignUnitToPlayer(int unitId, string playerId)
    {
        if (!Units.TryGetValue(unitId, out _))
            return false;

        if (!Players.TryGetValue(playerId, out var player))
            return false;

        if (!player.UnitIds.Contains(unitId))
        {
            player.UnitIds.Add(unitId);
        }

        return true;
    }

    /// <summary>
    /// Removes a unit assignment from a player.
    /// </summary>
    public bool UnassignUnitFromPlayer(int unitId, string playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
            return false;

        return player.UnitIds.Remove(unitId);
    }

    /// <summary>
    /// Gets all units assigned to a player.
    /// </summary>
    public List<Unit> GetPlayerUnits(string playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
            return new List<Unit>();

        return player.UnitIds
            .Where(unitId => Units.TryGetValue(unitId, out _))
            .Select(unitId => Units[unitId])
            .ToList();
    }

    /// <summary>
    /// Adds a unit to the session.
    /// </summary>
    public void AddUnit(int unitId, Unit unit)
    {
        if (unit == null) throw new ArgumentNullException(nameof(unit));
        Units[unitId] = unit;
    }

    /// <summary>
    /// Removes a unit from the session.
    /// </summary>
    public bool RemoveUnit(int unitId)
    {
        if (!Units.TryGetValue(unitId, out _))
            return false;

        // Remove from all player assignments
        foreach (var player in Players.Values)
        {
            player.UnitIds.Remove(unitId);
        }

        return Units.Remove(unitId);
    }

    /// <summary>
    /// Gets a unit by ID.
    /// </summary>
    public Unit? GetUnit(int unitId)
    {
        Units.TryGetValue(unitId, out var unit);
        return unit;
    }

    /// <summary>
    /// Creates and registers a user input request tied to a specific unit.
    /// </summary>
    public UserInputRequest RequestUnitInput(int unitId, string requestType, Dictionary<string, object?> inputSchema)
    {
        if (!Units.ContainsKey(unitId))
            throw new ArgumentException($"Unit '{unitId}' does not exist in this session.");

        var requestId = Guid.NewGuid().ToString();
        var request = new UserInputRequest(requestId, unitId, requestType, inputSchema);
        PendingInputRequests[requestId] = request;
        return request;
    }

    /// <summary>
    /// Resolves a pending input request.
    /// </summary>
    public bool ResolveInputRequest(string requestId, object? response)
    {
        if (!PendingInputRequests.TryGetValue(requestId, out var request))
            return false;

        request.Resolve(response);
        PendingInputRequests.Remove(requestId);
        ResolvedInputRequests[requestId] = request;
        return true;
    }

    /// <summary>
    /// Gets a pending input request by ID.
    /// </summary>
    public UserInputRequest? GetPendingInputRequest(string requestId)
    {
        PendingInputRequests.TryGetValue(requestId, out var request);
        return request;
    }

    /// <summary>
    /// Gets all pending input requests for a specific unit.
    /// </summary>
    public List<UserInputRequest> GetUnitPendingInputRequests(int unitId)
    {
        return PendingInputRequests.Values
            .Where(r => r.UnitId == unitId)
            .ToList();
    }

    /// <summary>
    /// Gets all pending input requests.
    /// </summary>
    public List<UserInputRequest> GetAllPendingInputRequests()
    {
        return PendingInputRequests.Values.ToList();
    }

    /// <summary>
    /// Gets all resolved input requests for a specific unit.
    /// </summary>
    public List<UserInputRequest> GetUnitResolvedInputRequests(int unitId)
    {
        return ResolvedInputRequests.Values
            .Where(r => r.UnitId == unitId)
            .ToList();
    }

    /// <summary>
    /// Gets the session duration.
    /// </summary>
    public TimeSpan Duration => DateTime.UtcNow - CreatedAt;

    /// <summary>
    /// Gets the total number of active players.
    /// </summary>
    public int ActivePlayerCount => Players.Count;

    /// <summary>
    /// Gets the total number of units in the session.
    /// </summary>
    public int TotalUnitCount => Units.Count;
}
