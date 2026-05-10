using System;
using System.Collections.Generic;
using System.Linq;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

public partial class GameSession
{
    /// <summary>
    /// Adds a player to the session.
    /// </summary>
    public void AddPlayer(Player player)
    {
        ArgumentNullException.ThrowIfNull(player, "Add Player: player");
        Players[player.Id] = player;
    }

    /// <summary>
    /// Removes a player from the session, and also removes all units that belong to that player.
    /// </summary>
    public bool RemovePlayer(string playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
            return false;

        foreach (var unitId in player.UnitIds.ToList())
            Units.Remove(unitId);

        return Players.Remove(playerId);
    }

    /// <summary>
    /// Returns the player with the given ID, or null if not found.
    /// </summary>
    public Player? GetPlayer(string playerId)
    {
        Players.TryGetValue(playerId, out var player);
        return player;
    }

    /// <summary>
    /// Assigns an existing session unit to a player.
    /// Returns false if either the unit or the player does not exist.
    /// </summary>
    public bool AssignUnitToPlayer(int unitId, string playerId)
    {
        if (!Units.ContainsKey(unitId))
            return false;

        if (!Players.TryGetValue(playerId, out var player))
            return false;

        if (!player.UnitIds.Contains(unitId))
            player.UnitIds.Add(unitId);

        return true;
    }

    /// <summary>
    /// Removes the unit assignment from a player without removing the unit from the session.
    /// Returns false if the player does not exist.
    /// </summary>
    public bool UnassignUnitFromPlayer(int unitId, string playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
            return false;

        return player.UnitIds.Remove(unitId);
    }

    /// <summary>
    /// Returns all units currently assigned to the given player.
    /// Returns an empty list if the player does not exist or has no units.
    /// </summary>
    public List<Unit> GetPlayerUnits(string playerId)
    {
        if (!Players.TryGetValue(playerId, out var player))
            return [];

        return [.. player.UnitIds
            .Where(unitId => Units.ContainsKey(unitId))
            .Select(unitId => Units[unitId])];
    }
}
