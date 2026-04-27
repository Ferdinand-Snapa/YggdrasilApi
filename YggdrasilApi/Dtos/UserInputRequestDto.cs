namespace YggdrasilApi.Dtos;

public class UserInputRequestDto
{
    public string Id { get; set; } = null!;
    public int UnitId { get; set; }
    public string RequestType { get; set; } = null!;
    public Dictionary<string, object?> InputSchema { get; set; } = new();
    public object? Response { get; set; }
    public bool IsResolved { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public TimeSpan ElapsedTime { get; set; }
}
