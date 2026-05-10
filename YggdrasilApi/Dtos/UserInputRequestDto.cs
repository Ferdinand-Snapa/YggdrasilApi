using YggdrasilApi.Models;

namespace YggdrasilApi.Dtos;

/// <summary>
/// API / SignalR transfer object for a <see cref="YggdrasilApi.GameLogick.UserInputRequest"/>.
/// </summary>
public class UserInputRequestDto
{
    public string Id { get; set; } = null!;
    public int UnitId { get; set; }
    public string RequestType { get; set; } = null!;

    /// <summary>
    /// Typed schema describing every field the player must fill in.
    /// Key = field name; Value = <see cref="InputField"/> with type, rank, and constraint.
    /// </summary>
    public Dictionary<string, InputField> Schema { get; set; } = new();

    /// <summary>
    /// The raw response as received from the client (null until resolved).
    /// For the validated typed version see the server-side
    /// <c>UserInputRequest.TypedResponse</c>.
    /// </summary>
    public object? Response { get; set; }

    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}
