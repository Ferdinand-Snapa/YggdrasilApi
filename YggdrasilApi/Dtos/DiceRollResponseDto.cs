namespace YggdrasilApi.Dtos;

public class DiceRollResponseDto
{
    /// <summary>
    /// Dictionary where key is number of sides and value is array of rolled values.
    /// Example: { "6": [3, 5], "20": [18] }
    /// </summary>
    public Dictionary<string, int[]> Rolls { get; set; } = new();
}
