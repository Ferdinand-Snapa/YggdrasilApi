using System;
using System.Collections.Generic;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

/// <summary>
/// Represents a player in a game session.
/// </summary>
public class Player
{
    public string Id { get; set; }
    public string Name { get; set; }
    public List<int> UnitIds { get; set; } = new List<int>();

    public Player(string id, string name)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Name = name ?? throw new ArgumentNullException(nameof(name));
    }
}
