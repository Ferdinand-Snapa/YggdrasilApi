namespace YggdrasilApi.Models
{
    public class Graph
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<PortDefenition> InputPorts { get; set; } = new List<PortDefenition>();
        public List<PortDefenition> OutputPorts { get; set; } = new List<PortDefenition>();
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Connection> Connections { get; set; } = new List<Connection>();
    }
}
