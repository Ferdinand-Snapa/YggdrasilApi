namespace YggdrasilApi.Models
{
    public class Graph
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Node> Nodes { get; set; } = new List<Node>();
        public Dictionary<int, Connection> Connections { get; set; } = new Dictionary<int, Connection>();
    }
}
