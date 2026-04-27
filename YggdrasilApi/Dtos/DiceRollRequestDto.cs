namespace YggdrasilApi.Dtos;

public class DiceRollRequestDto
{
    public string RequestId { get; set; } = null!;
    public int UnitId { get; set; }
    public Dictionary<int, int> DiceSpec { get; set; } = new();
    public DateTime CreatedAt { get; set; }
}
