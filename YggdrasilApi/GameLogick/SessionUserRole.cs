namespace YggdrasilApi.GameLogick;

/// <summary>
/// Defines the roles a user can have in a game session.
/// </summary>
public enum SessionUserRole
{
    /// <summary>
    /// User has applied to join but hasn't been approved yet.
    /// </summary>
    Pending,

    /// <summary>
    /// User can only view the session, no interaction.
    /// </summary>
    Spectator,

    /// <summary>
    /// User can participate in the game fully.
    /// </summary>
    Player,

    /// <summary>
    /// User can manage the session, approve users, and manage roles.
    /// </summary>
    CoLeader,

    /// <summary>
    /// User created the session and has full control.
    /// </summary>
    SessionLeader,

    /// <summary>
    /// User has been blocked from the session.
    /// </summary>
    BlackListed
}
