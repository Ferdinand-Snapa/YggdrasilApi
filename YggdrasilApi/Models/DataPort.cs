namespace YggdrasilApi.Models;

public class DataPort : PortDefenition
{
    public string PortType { get; set; } = "any"; // Default: accepts any type
}
