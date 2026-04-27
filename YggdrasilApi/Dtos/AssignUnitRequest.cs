namespace YggdrasilApi.Dtos;

public class AssignUnitRequest
{
    public int UnitId { get; set; }
    public string PlayerId { get; set; } = null!;
}
