namespace YggdrasilApi.GameLogick;

/// <summary>
/// Represents a user in a game session.
/// </summary>
public class SessionUser
{
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
    public SessionUserRole Role { get; set; } = SessionUserRole.Pending;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public string? ConnectionId { get; set; }
    public bool IsConnected { get; set; }

    public SessionUser(string userId, string userName)
    {
        UserId = userId;
        UserName = userName;
    }
}
