namespace YggdrasilApi.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int[] Derives { get; set; } = System.Array.Empty<int>();
        public List<Decleration> Declerations { get; set; } = new List<Decleration>();
        public List<Graph> Graphs { get; set; } = new List<Graph>();
    }
}
