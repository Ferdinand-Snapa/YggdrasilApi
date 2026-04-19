namespace YggdrasilApi.Dtos;

public class CreateConnectionRequest
{
    public int FromNodeId { get; set; }
    public int FromPortId { get; set; }
    public int ToNodeId { get; set; }
    public int ToPortId { get; set; }
}
