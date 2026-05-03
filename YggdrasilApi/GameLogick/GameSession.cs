using System;
using System.Collections.Generic;
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

    /// <summary>How long the session has been running.</summary>
    public TimeSpan Duration => DateTime.UtcNow - CreatedAt;

    /// <summary>Number of players currently in the session.</summary>
    public int ActivePlayerCount => Players.Count;

    /// <summary>Total number of units registered in the session.</summary>
    public int TotalUnitCount => Units.Count;
}
