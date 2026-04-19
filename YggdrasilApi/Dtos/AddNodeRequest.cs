namespace YggdrasilApi.Dtos;

public class AddNodeRequest
{
    public int PositionX { get; set; }
    public int PositionY { get; set; }
    public string Type { get; set; } = string.Empty;
}
