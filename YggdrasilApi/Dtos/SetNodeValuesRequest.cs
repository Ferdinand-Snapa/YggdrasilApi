using System.Collections.Generic;

namespace YggdrasilApi.Dtos;

public class SetNodeValuesRequest
{
    public Dictionary<string, object?> Values { get; set; } = new Dictionary<string, object?>();
}
