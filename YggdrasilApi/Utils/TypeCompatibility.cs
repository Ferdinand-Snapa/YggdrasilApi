using YggdrasilApi.Models;

namespace YggdrasilApi.Utils;

/// <summary>
/// Determines whether a value produced on one graph port can flow into another.
/// </summary>
public static class TypeCompatibility
{
    // ─────────────────────────────────────────────────────────────────────────
    // Typed API — preferred; uses FieldType directly
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true when a value of <paramref name="output"/> type can be accepted by
    /// a port that expects <paramref name="input"/> type.
    ///
    /// Rules:
    /// <list type="bullet">
    ///   <item><see cref="FieldType.Undefined"/> on the input side accepts everything.</item>
    ///   <item>Identical types are always compatible.</item>
    ///   <item>Any conversion defined in <see cref="FieldValue"/>'s conversion table is accepted.</item>
    /// </list>
    /// </summary>
    public static bool IsCompatible(FieldType output, FieldType input)
    {
        // An Undefined input port accepts any type.
        if (input == FieldType.Undefined) return true;

        // Exact match.
        if (input == output) return true;

        // A defined conversion exists (e.g. Int → Float, Bool → Int).
        return FieldValue.CanConvert(output, input);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Legacy string API — kept for backward compatibility with DataPort.PortType
    // ─────────────────────────────────────────────────────────────────────────

    // Maps the string type names used by DataPort to their FieldType equivalents.
    private static readonly IReadOnlyDictionary<string, FieldType> StringToFieldType =
        new Dictionary<string, FieldType>(StringComparer.OrdinalIgnoreCase)
        {
            ["float"] = FieldType.Float,
            ["int"] = FieldType.Int,
            ["bool"] = FieldType.Bool,
            ["boolean"] = FieldType.Bool,
            ["string"] = FieldType.String,
            ["text"] = FieldType.String,
            ["dice"] = FieldType.Dice,
            ["tag"] = FieldType.Tag,
            ["unit"] = FieldType.Unit,
            ["reference"] = FieldType.Reference,
            ["ref"] = FieldType.Reference,
            ["number"] = FieldType.Float,   // legacy alias
            ["any"] = FieldType.Undefined,
        };

    /// <summary>
    /// Returns true when a value produced by a port of string type <paramref name="outputType"/>
    /// can flow into a port that expects string type <paramref name="inputType"/>.
    /// Unrecognised type strings are treated as <see cref="FieldType.Undefined"/> (accept-all).
    /// </summary>
    public static bool IsCompatible(string outputType, string inputType)
    {
        var output = StringToFieldType.GetValueOrDefault(outputType, FieldType.Undefined);
        var input = StringToFieldType.GetValueOrDefault(inputType, FieldType.Undefined);
        return IsCompatible(output, input);
    }

    /// <summary>
    /// Converts a DataPort string type name to its <see cref="FieldType"/> equivalent.
    /// Returns <see cref="FieldType.Undefined"/> for unrecognised names (behaves as "any").
    /// </summary>
    public static FieldType ToFieldType(string portTypeName)
        => StringToFieldType.GetValueOrDefault(portTypeName, FieldType.Undefined);
}
