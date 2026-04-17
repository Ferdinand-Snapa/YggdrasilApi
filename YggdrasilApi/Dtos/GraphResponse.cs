using YggdrasilApi.Models;

namespace YggdrasilApi.Dtos
{
    public class GraphResponse
    {
        // public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Connection> Connections { get; set; } = new List<Connection>();
    }
}
