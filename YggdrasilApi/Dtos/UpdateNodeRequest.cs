namespace YggdrasilApi.Dtos;

public class UpdateNodeRequest
{
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public string Type { get; set; } = string.Empty;
}
