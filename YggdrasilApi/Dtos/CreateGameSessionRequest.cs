namespace YggdrasilApi.Dtos;

public class CreateGameSessionRequest
{
    public string SessionId { get; set; } = null!;
    public string UserId { get; set; } = null!;
    public string UserName { get; set; } = null!;
}
