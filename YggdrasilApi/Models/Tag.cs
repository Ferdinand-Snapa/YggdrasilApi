using System.Drawing;

namespace YggdrasilApi.Models;

public class Tag
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Color { get; set; } = "#FFFFFF";
}
