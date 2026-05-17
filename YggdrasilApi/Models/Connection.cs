namespace YggdrasilApi.Models
{
    public class Connection
    {
        public int Id { get; set; }

        public FieldType? ConnectionType { get; set; } = null;
        public int TypeRank { get; set; } = 0;

        public string FromNodeId { get; set; } = string.Empty;
        public string FromPortId { get; set; } = string.Empty;
        public string ToNodeId { get; set; } = string.Empty;
        public string ToPortId { get; set; } = string.Empty;
    }
}
