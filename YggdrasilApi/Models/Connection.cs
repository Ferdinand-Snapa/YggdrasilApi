namespace YggdrasilApi.Models
{
    public struct Connection
    {
        public int FromNodeId { get; set; }
        public int FromPortId { get; set; }
        public int ToNodeId { get; set; }
        public int ToPortId { get; set; }
    }
}
