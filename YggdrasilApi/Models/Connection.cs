namespace YggdrasilApi.Models
{
    public class Connection
    {
        public int Id { get; set; }
        public int FromNodeId { get; set; }
        public int FromPortId { get; set; }
        public int ToNodeId { get; set; }
        public int ToPortId { get; set; }
    }
}
