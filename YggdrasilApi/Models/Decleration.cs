namespace YggdrasilApi.Models
{
    public enum DeclerationType
    {
        Float, Dice, String, Bool, Undefined, Unit
    }
    public class Decleration
    {
        public int Id { get; set; }

        public string Name { get; set; } = string.Empty;

        public DeclerationType Type { get; set; } = DeclerationType.Undefined;
    }
}
