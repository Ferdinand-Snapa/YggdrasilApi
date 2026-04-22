using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace YggdrasilApi.Models;

/// <summary>
/// Represents a collection of dice rolls by number of sides.
/// Example: { 6: 2, 20: 1 } means two d6 and one d20.
/// </summary>
public class Dice
{
    // Key: number of sides, Value: count of dice
    public Dictionary<int, int> Rolls { get; set; } = new Dictionary<int, int>();

    public Dice() { }

    public Dice(Dictionary<int, int> rolls)
    {
        Rolls = rolls ?? new Dictionary<int, int>();
    }

    /// <summary>
    /// Create a Dice instance from a JSON object string like { "6": 2, "20": 1 }
    /// </summary>
    public static Dice? FromJson(string json)
    {
        try
        {
            using var doc = JsonDocument.Parse(json);
            var root = doc.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return null;

            var rolls = new Dictionary<int, int>();
            foreach (var prop in root.EnumerateObject())
            {
                if (!int.TryParse(prop.Name, out var sides) || sides <= 0) return null;
                if (!prop.Value.TryGetInt32(out var count) || count < 0) return null;
                rolls[sides] = count;
            }

            return new Dice(rolls);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// Create a Dice instance from a JsonElement representing a dice object
    /// </summary>
    public static Dice? FromJsonElement(JsonElement element)
    {
        if (element.ValueKind != JsonValueKind.Object) return null;

        var rolls = new Dictionary<int, int>();
        foreach (var prop in element.EnumerateObject())
        {
            if (!int.TryParse(prop.Name, out var sides) || sides <= 0) return null;
            if (!prop.Value.TryGetInt32(out var count) || count < 0) return null;
            rolls[sides] = count;
        }

        return new Dice(rolls);
    }

    public static Dice operator +(Dice a, Dice b)
    {
        var result = new Dictionary<int, int>(a.Rolls);
        foreach (var kv in b.Rolls)
        {
            if (result.ContainsKey(kv.Key))
                result[kv.Key] += kv.Value;
            else
                result[kv.Key] = kv.Value;
        }
        return new Dice(result);
    }

    public static Dice operator -(Dice a, Dice b)
    {
        var result = new Dictionary<int, int>(a.Rolls);
        foreach (var kv in b.Rolls)
        {
            if (result.ContainsKey(kv.Key))
            {
                result[kv.Key] -= kv.Value;
                if (result[kv.Key] < 0) result[kv.Key] = 0;
            }
        }
        return new Dice(result);
    }
    // TODO add a random controller and ability to specify roll 
    public int RollDice()
    {
        var rand = new Random();
        int total = 0;
        foreach (var kv in Rolls)
        {
            int sides = kv.Key;
            int count = kv.Value;
            for (int i = 0; i < count; i++)
                total += rand.Next(1, sides + 1);
            
        }
        return total;
    }

    /// <summary>
    /// Convert this Dice to a JSON object string
    /// </summary>
    public string ToJson()
    {
        var dict = Rolls.ToDictionary(k => k.Key.ToString(), v => (object)v.Value);
        return JsonSerializer.Serialize(dict);
    }

    /// <summary>
    /// Get total number of dice
    /// </summary>
    public int TotalCount => Rolls.Values.Sum();

    /// <summary>
    /// Get the combined notation string like "2d6+1d20"
    /// </summary>
    public string ToNotation()
    {
        if (Rolls.Count == 0) return "0d0";
        var parts = Rolls.OrderBy(kv => kv.Key).Select(kv => $"{kv.Value}d{kv.Key}");
        return string.Join("+", parts);
    }

    public override string ToString() => ToNotation();
}
