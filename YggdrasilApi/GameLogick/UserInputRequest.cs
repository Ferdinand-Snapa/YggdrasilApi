using System;
using System.Collections.Generic;

namespace YggdrasilApi.GameLogick;

/// <summary>
/// Represents a request for user input tied to a unit.
/// The owner of the unit (its controlling player) receives the request.
/// </summary>
public class UserInputRequest
{
    public string Id { get; set; }
    public int UnitId { get; set; }
    public string RequestType { get; set; }
    public Dictionary<string, object?> InputSchema { get; set; } = new Dictionary<string, object?>();
    public object? Response { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    public UserInputRequest(string id, int unitId, string requestType, Dictionary<string, object?> inputSchema)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        RequestType = requestType ?? throw new ArgumentNullException(nameof(requestType));
        InputSchema = inputSchema ?? new Dictionary<string, object?>();
        UnitId = unitId;
        CreatedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Resolves the input request with the provided response.
    /// </summary>
    public void Resolve(object? response)
    {
        Response = response;
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the time elapsed since the request was created.
    /// </summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - CreatedAt;
}
