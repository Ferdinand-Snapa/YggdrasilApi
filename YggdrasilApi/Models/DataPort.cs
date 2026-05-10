namespace YggdrasilApi.Models;

/// <summary>
/// A graph port that carries a typed data value (as opposed to a <see cref="FlowPort"/>,
/// which carries execution control).
/// </summary>
public class DataPort : PortDefenition
{
    /// <summary>
    /// The expected <see cref="FieldType"/> of the value on this port.
    /// <see cref="FieldType.Undefined"/> means "accepts any type" (equivalent to the old <c>"any"</c> string).
    /// </summary>
    public FieldType PortType { get; set; } = FieldType.Undefined;
}
