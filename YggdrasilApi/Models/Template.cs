namespace YggdrasilApi.Models
{
    public class Template
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int[] Derives { get; set; } = [];
        public Dictionary<int, Decleration> Declerations { get; set; } = new Dictionary<int, Decleration>();
        public Dictionary<int, Graph> Graphs { get; set; } = new Dictionary<int, Graph>();

        
    }
}
