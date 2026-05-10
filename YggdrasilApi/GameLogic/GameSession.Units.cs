using System;
using System.Collections.Generic;
using System.Linq;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

public partial class GameSession
{
    // ── Unit management ────────────────────────────────────────────────────────────

    /// <summary>
    /// Registers a unit in the session under the given ID.
    /// </summary>
    public void AddUnit(int unitId, Unit unit)
    {
        ArgumentNullException.ThrowIfNull(unit, "Add unit: unit");
        Units[unitId] = unit;
    }

    /// <summary>
    /// Removes a unit from the session and clears its assignment from every player.
    /// Returns false if the unit does not exist.
    /// </summary>
    public bool RemoveUnit(int unitId)
    {
        if (!Units.ContainsKey(unitId))
            return false;

        foreach (var player in Players.Values)
            player.UnitIds.Remove(unitId);

        return Units.Remove(unitId);
    }

    /// <summary>
    /// Returns the unit with the given ID, or null if not found.
    /// </summary>
    public Unit? GetUnit(int unitId)
    {
        Units.TryGetValue(unitId, out var unit);
        return unit;
    }

    // ── Input-request management ───────────────────────────────────────────────────

    /// <summary>
    /// Creates and registers a pending input request tied to a specific unit.
    /// Throws if the unit does not exist in the session.
    /// </summary>
    public UserInputRequest RequestUnitInput(int unitId, string requestType,
                                             Dictionary<string, InputField> schema)
    {
        if (!Units.ContainsKey(unitId))
            throw new ArgumentException($"Unit '{unitId}' does not exist in this session.");

        var requestId = Guid.NewGuid().ToString();
        var request = new UserInputRequest(requestId, unitId, schema);
        PendingInputRequests[requestId] = request;
        return request;
    }

    /// <summary>
    /// Resolves a pending input request and moves it to the resolved list.
    /// Returns false if the request ID is not found in the pending list.
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
    /// Returns a single pending input request by ID, or null if not found.
    /// </summary>
    public UserInputRequest? GetPendingInputRequest(string requestId)
    {
        PendingInputRequests.TryGetValue(requestId, out var request);
        return request;
    }

    /// <summary>
    /// Returns all pending input requests across the whole session.
    /// </summary>
    public List<UserInputRequest> GetAllPendingInputRequests()
        => PendingInputRequests.Values.ToList();

    /// <summary>
    /// Returns all pending input requests for a specific unit.
    /// </summary>
    public List<UserInputRequest> GetUnitPendingInputRequests(int unitId)
        => PendingInputRequests.Values.Where(r => r.UnitId == unitId).ToList();

    /// <summary>
    /// Returns all resolved input requests for a specific unit.
    /// </summary>
    public List<UserInputRequest> GetUnitResolvedInputRequests(int unitId)
        => [.. ResolvedInputRequests.Values.Where(r => r.UnitId == unitId)];
}
