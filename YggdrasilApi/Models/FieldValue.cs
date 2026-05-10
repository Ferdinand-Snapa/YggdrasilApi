using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json;

namespace YggdrasilApi.Models;

/// <summary>
/// A strongly-typed, immutable, DB-serializable value that flows through graph ports
/// and is stored on unit declarations.
///
/// <para><b>Creating scalars:</b></para>
/// <code>
/// FieldValue.Float(1.5f)
/// FieldValue.Int(42)
/// FieldValue.Bool(true)
/// FieldValue.Text("hello")
/// FieldValue.FromDice(new Dice(...))
/// FieldValue.Tag(tagId)
/// FieldValue.FromUnit(unit)
/// FieldValue.Reference(new FieldReference(unitId, declarationId))
/// FieldValue.Undefined
/// </code>
///
/// <para><b>Creating arrays (any nesting depth):</b></para>
/// <code>
/// // Flat array of floats (Rank = 1):
/// FieldValue.ArrayOf(FieldType.Float, [Float(1f), Float(2f)])
///
/// // 2-D array of floats (Rank = 2, array of float arrays):
/// var row = FieldValue.ArrayOf(FieldType.Float, [Float(1f), Float(2f)]);
/// FieldValue.ArrayOf(FieldType.Float, [row, row])
///
/// // Empty array of scalars / of sub-arrays:
/// FieldValue.EmptyArray(FieldType.Int)               // Rank 1
/// FieldValue.EmptyArray(FieldType.Int, elementRank: 1) // Rank 2
/// </code>
///
/// <para><b>DB persistence (single TEXT column):</b></para>
/// <code>
/// string stored = value.Serialize();
/// FieldValue loaded = FieldValue.Deserialize(stored);
/// </code>
///
/// <para><b>Type conversion:</b></para>
/// <code>
/// if (value.TryConvertTo(FieldType.Float, out var asFloat)) { ... }
/// FieldValue asFloat = value.ConvertTo(FieldType.Float); // throws if incompatible
/// </code>
/// </summary>
public sealed class FieldValue
{
    // ─────────────────────────────────────────────────────────────────────────
    // Core state
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The base scalar type of this value, regardless of array depth.</summary>
    public FieldType Type { get; }

    /// <summary>
    /// Nesting depth of this value.
    /// <list type="bullet">
    ///   <item><b>0</b> — scalar (a single value).</item>
    ///   <item><b>1</b> — flat array of scalars.</item>
    ///   <item><b>2</b> — array of flat arrays (2-D / jagged).</item>
    ///   <item><b>N</b> — N-dimensional jagged array.</item>
    /// </list>
    /// </summary>
    public int Rank { get; }

    /// <summary>True when <see cref="Rank"/> &gt; 0 (i.e. this value is any kind of array).</summary>
    public bool IsArray => Rank > 0;

    // Internal storage. Scalars hold the typed value directly; arrays hold List<FieldValue>;
    // Undefined holds DBNull.Value.
    private readonly object _inner;

    // ─────────────────────────────────────────────────────────────────────────
    // Undefined singleton
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>The single canonical undefined / null field value.</summary>
    public static readonly FieldValue Undefined = new(FieldType.Undefined, DBNull.Value);

    // ─────────────────────────────────────────────────────────────────────────
    // Private constructor — use the static factories below
    // ─────────────────────────────────────────────────────────────────────────

    private FieldValue(FieldType type, object inner, int rank = 0)
    {
        Type = type;
        _inner = inner;
        Rank = rank;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Scalar factory methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>Creates a <see cref="FieldType.Float"/> value.</summary>
    public static FieldValue Float(float value) => new(FieldType.Float, value);

    /// <summary>Creates an <see cref="FieldType.Int"/> value.</summary>
    public static FieldValue Int(int value) => new(FieldType.Int, value);

    /// <summary>Creates a <see cref="FieldType.Bool"/> value.</summary>
    public static FieldValue Bool(bool value) => new(FieldType.Bool, value);

    /// <summary>Creates a <see cref="FieldType.String"/> value.</summary>
    public static FieldValue Text(string value) => new(FieldType.String, value ?? string.Empty);

    /// <summary>Creates a <see cref="FieldType.Dice"/> value. Prefixed with "From" to avoid shadowing the <see cref="Dice"/> class.</summary>
    public static FieldValue FromDice(Dice value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(FieldType.Dice, value);
    }

    /// <summary>Creates a <see cref="FieldType.Tag"/> value from a tag's integer ID.</summary>
    public static FieldValue Tag(int tagId) => new(FieldType.Tag, tagId);

    /// <summary>Creates a <see cref="FieldType.Unit"/> value. Prefixed with "From" to avoid shadowing the <see cref="Unit"/> class.</summary>
    public static FieldValue FromUnit(Unit value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(FieldType.Unit, value);
    }

    /// <summary>
    /// Creates a <see cref="FieldType.Reference"/> value pointing to a unit
    /// (and optionally a declaration) that already exists in the session.
    /// </summary>
    public static FieldValue Reference(FieldReference value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return new(FieldType.Reference, value);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Array factory methods
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Creates an array FieldValue whose elements all have <paramref name="elementType"/>.
    /// Elements may be scalars (Rank 0) <i>or</i> sub-arrays — as long as every element
    /// has the same <see cref="Rank"/> and the same base <see cref="Type"/>.
    /// The resulting value has <c>Rank = elementRank + 1</c>.
    /// </summary>
    /// <example>
    /// Flat array of floats (Rank 1):
    /// <code>ArrayOf(FieldType.Float, [Float(1f), Float(2f)])</code>
    /// 2-D array of floats (Rank 2):
    /// <code>
    /// var row = ArrayOf(FieldType.Float, [Float(1f), Float(2f)]);
    /// ArrayOf(FieldType.Float, [row, row]);
    /// </code>
    /// </example>
    public static FieldValue ArrayOf(FieldType elementType, IEnumerable<FieldValue> elements)
    {
        var list = elements.ToList();
        if (list.Count == 0)
            return EmptyArray(elementType);

        int elementRank = list[0].Rank;
        var bad = list.FirstOrDefault(e => e.Type != elementType || e.Rank != elementRank);
        if (bad is not null)
            throw new ArgumentException(
                $"All elements must have Type={elementType} and Rank={elementRank}, " +
                $"but found Type={bad.Type} Rank={bad.Rank}.",
                nameof(elements));

        return new FieldValue(elementType, new List<FieldValue>(list), rank: elementRank + 1);
    }

    /// <summary>
    /// Creates an empty array of the given element type.
    /// </summary>
    /// <param name="elementType">The base scalar type of the elements.</param>
    /// <param name="elementRank">
    /// The <see cref="Rank"/> of each element.
    /// 0 (default) means an array of scalars (result Rank = 1).
    /// 1 means an array of flat arrays (result Rank = 2), etc.
    /// </param>
    public static FieldValue EmptyArray(FieldType elementType, int elementRank = 0)
        => new(elementType, new List<FieldValue>(), rank: elementRank + 1);

    // ─────────────────────────────────────────────────────────────────────────
    // Typed accessors — throw on type mismatch (fail-fast)
    // ─────────────────────────────────────────────────────────────────────────

    public float AsFloat() => ScalarOf<float>(FieldType.Float);
    public int AsInt() => ScalarOf<int>(FieldType.Int);
    public bool AsBool() => ScalarOf<bool>(FieldType.Bool);
    public string AsText() => ScalarOf<string>(FieldType.String);
    public Dice AsDice() => ScalarOf<Dice>(FieldType.Dice);
    public int AsTagId() => ScalarOf<int>(FieldType.Tag);
    public Unit AsUnit() => ScalarOf<Unit>(FieldType.Unit);
    public FieldReference AsReference() => ScalarOf<FieldReference>(FieldType.Reference);

    /// <summary>
    /// Returns the list of direct child elements.
    /// For a Rank-1 value those are scalars; for Rank-2 they are Rank-1 sub-arrays, etc.
    /// Throws if this is a scalar (Rank = 0).
    /// </summary>
    public IReadOnlyList<FieldValue> AsArray()
    {
        if (Rank == 0)
            throw new InvalidOperationException(
                $"This FieldValue (Type={Type}) is a scalar (Rank=0), not an array.");
        return (IReadOnlyList<FieldValue>)_inner;
    }

    // Private helper: assert scalar (Rank=0) and unbox in one step.
    private T ScalarOf<T>(FieldType expected)
    {
        if (Rank > 0)
            throw new InvalidOperationException(
                $"This FieldValue is a Rank-{Rank} array of {Type}, not a scalar {expected}.");
        if (Type != expected)
            throw new InvalidOperationException(
                $"Expected FieldType.{expected} but this value has FieldType.{Type}.");
        return (T)_inner;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Type conversion
    // ─────────────────────────────────────────────────────────────────────────

    // Static table of all defined conversions (From, To) → converter.
    // Conversions not in this table are not supported.
    private static readonly IReadOnlyDictionary<(FieldType From, FieldType To), Func<FieldValue, FieldValue>>
        ConversionTable = new Dictionary<(FieldType, FieldType), Func<FieldValue, FieldValue>>
        {
            // ── Numeric widenings (always safe) ──────────────────────────────────
            [(FieldType.Int, FieldType.Float)] = v => Float((float)v.AsInt()),

            // ── Numeric narrowings (lossy: truncates toward zero) ─────────────────
            [(FieldType.Float, FieldType.Int)] = v => Int((int)v.AsFloat()),

            // ── Bool → numeric ───────────────────────────────────────────────────
            [(FieldType.Bool, FieldType.Int)] = v => Int(v.AsBool() ? 1 : 0),
            [(FieldType.Bool, FieldType.Float)] = v => Float(v.AsBool() ? 1f : 0f),

            // ── Anything → String ─────────────────────────────────────────────────
            [(FieldType.Float, FieldType.String)] = v => Text(v.AsFloat().ToString(CultureInfo.InvariantCulture)),
            [(FieldType.Int, FieldType.String)] = v => Text(v.AsInt().ToString()),
            [(FieldType.Bool, FieldType.String)] = v => Text(v.AsBool() ? "true" : "false"),
            [(FieldType.Dice, FieldType.String)] = v => Text(v.AsDice().ToNotation()),
            [(FieldType.Tag, FieldType.String)] = v => Text(v.AsTagId().ToString()),
            [(FieldType.Unit, FieldType.String)] = v => Text(v.AsUnit().ToJson()),

            // ── ID extractions ───────────────────────────────────────────────────
            [(FieldType.Tag, FieldType.Int)] = v => Int(v.AsTagId()),
            [(FieldType.Unit, FieldType.Int)] = v => Int(v.AsUnit().TemplateId),

            // ── Dice → numeric (non-deterministic: rolls on each call) ────────────
            [(FieldType.Dice, FieldType.Int)] = v => Int(v.AsDice().RollDice()),
            [(FieldType.Dice, FieldType.Float)] = v => Float((float)v.AsDice().RollDice()),
        };

    /// <summary>
    /// Returns true if a value of type <paramref name="from"/> can be converted to <paramref name="to"/>.
    /// This is the static equivalent of <see cref="CanConvertTo"/> for use in port-compatibility checks.
    /// </summary>
    public static bool CanConvert(FieldType from, FieldType to)
    {
        if (to == FieldType.Undefined) return true;
        if (from == to) return true;
        return ConversionTable.ContainsKey((from, to));
    }

    /// <summary>True if this scalar value (Rank = 0) can be converted to <paramref name="target"/>.
    /// Array values (Rank &gt; 0) are never directly convertible.</summary>
    public bool CanConvertTo(FieldType target)
    {
        if (Rank > 0) return false;
        return Type == target
            || target == FieldType.Undefined
            || ConversionTable.ContainsKey((Type, target));
    }

    /// <summary>
    /// Tries to convert this value to <paramref name="target"/>.
    /// Returns true and sets <paramref name="result"/> on success.
    /// Returns false (leaving <paramref name="result"/> unchanged) when no conversion is defined.
    /// Array values (Rank &gt; 0) always return false.
    /// </summary>
    public bool TryConvertTo(FieldType target, out FieldValue result)
    {
        if (Rank > 0) { result = this; return false; }
        if (Type == target || target == FieldType.Undefined) { result = this; return true; }

        if (ConversionTable.TryGetValue((Type, target), out var converter))
        {
            result = converter(this);
            return true;
        }

        result = this;
        return false;
    }

    /// <summary>
    /// Converts this value to <paramref name="target"/>.
    /// Throws <see cref="InvalidOperationException"/> if no conversion is defined.
    /// </summary>
    public FieldValue ConvertTo(FieldType target)
    {
        if (!TryConvertTo(target, out var result))
            throw new InvalidOperationException(
                $"No conversion defined from FieldType.{Type} to FieldType.{target}.");
        return result;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DB serialization
    // ─────────────────────────────────────────────────────────────────────────
    //
    // Stored format:   TYPE_TAG:JSON_PAYLOAD
    //
    //   TYPE_TAG   = lower-case FieldType name, with "[]" suffix for arrays.
    //   JSON_PAYLOAD = always valid JSON so the column is easy to inspect:
    //
    //     float / int / tag → JSON number
    //     bool              → JSON true / false
    //     string            → JSON string  (quoted + escaped)
    //     dice              → JSON object  e.g. {"6":2,"20":1}
    //     unit              → JSON object  {"TemplateId":N,"Values":{...}}
    //     reference         → JSON object  {"UnitId":N,"DeclarationId":N|null}
    //     undefined         → JSON null
    //     array             → JSON array   of the element payloads above
    //
    // Examples:
    //   "float:1.5"
    //   "string:\"hello world\""
    //   "dice:{\"6\":2,\"20\":1}"
    //   "reference:{\"UnitId\":5,\"DeclarationId\":null}"
    //   "float[]:[1.0,2.5,3.0]"
    //   "string[]:[\"a\",\"b\",\"c\"]"

    /// <summary>Serializes this value to a compact string suitable for a DB TEXT / VARCHAR column.</summary>
    /// <remarks>
    /// Nested arrays use repeated <c>[]</c> suffixes matching the rank, e.g.:
    /// <c>float[]:[1.0,2.0]</c> (Rank 1), <c>float[][]:[<![CDATA[[1.0,2.0],[3.0]]]></c> (Rank 2).
    /// </remarks>
    public string Serialize()
    {
        var brackets = string.Concat(Enumerable.Repeat("[]", Rank));
        var tag = Type.ToString().ToLowerInvariant() + brackets;
        return $"{tag}:{BuildPayload(this)}";
    }

    // Dispatches to the correct builder based on rank.
    private static string BuildPayload(FieldValue value) =>
        value.Rank > 0 ? value.BuildArrayPayload() : BuildScalarPayload(value.Type, value._inner);

    private string BuildArrayPayload()
    {
        var parts = ((IReadOnlyList<FieldValue>)_inner).Select(BuildPayload);
        return "[" + string.Join(",", parts) + "]";
    }

    private static string BuildScalarPayload(FieldType type, object inner) => type switch
    {
        FieldType.Float => JsonSerializer.Serialize((float)inner),
        FieldType.Int => JsonSerializer.Serialize((int)inner),
        FieldType.Bool => JsonSerializer.Serialize((bool)inner),
        FieldType.String => JsonSerializer.Serialize((string)inner),
        FieldType.Dice => ((Dice)inner).ToJson(),
        FieldType.Tag => JsonSerializer.Serialize((int)inner),
        FieldType.Unit => ((Unit)inner).ToJson(),
        FieldType.Reference => JsonSerializer.Serialize((FieldReference)inner),
        FieldType.Undefined => "null",
        _ => throw new InvalidOperationException($"Cannot serialize unknown FieldType: {type}")
    };

    /// <summary>
    /// Deserializes a string previously produced by <see cref="Serialize"/>.
    /// Throws <see cref="FormatException"/> for malformed input.
    /// </summary>
    public static FieldValue Deserialize(string serialized)
    {
        ArgumentException.ThrowIfNullOrEmpty(serialized);

        var colonIdx = serialized.IndexOf(':');
        if (colonIdx < 0)
            throw new FormatException($"Invalid FieldValue string (missing ':'): \"{serialized}\"");

        var tag = serialized[..colonIdx];
        var payload = serialized[(colonIdx + 1)..];

        // Count and strip trailing [] pairs to determine rank.
        int rank = 0;
        while (tag.EndsWith("[]", StringComparison.Ordinal))
        {
            rank++;
            tag = tag[..^2];
        }

        if (!Enum.TryParse<FieldType>(tag, ignoreCase: true, out var type))
            throw new FormatException($"Unknown FieldType tag \"{tag}\" in: \"{serialized}\"");

        return rank > 0 ? ParseArrayPayload(type, rank, payload)
                        : ParseScalarPayload(type, payload);
    }

    // rank is the depth of the value being parsed (>= 1).
    // rank == 1 → elements are scalars; rank > 1 → elements are sub-arrays of rank-1.
    private static FieldValue ParseArrayPayload(FieldType elementType, int rank, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        if (doc.RootElement.ValueKind != JsonValueKind.Array)
            throw new FormatException(
                $"Expected JSON array for Rank-{rank} {elementType} value, got {doc.RootElement.ValueKind}.");

        var elements = doc.RootElement
            .EnumerateArray()
            .Select(el => rank == 1
                ? ParseScalarElement(elementType, el)
                : ParseArrayPayload(elementType, rank - 1, el.GetRawText()))
            .ToList();

        return new FieldValue(elementType, elements, rank: rank);
    }

    private static FieldValue ParseScalarPayload(FieldType type, string payload)
    {
        using var doc = JsonDocument.Parse(payload);
        return ParseScalarElement(type, doc.RootElement);
    }

    private static FieldValue ParseScalarElement(FieldType type, JsonElement el) => type switch
    {
        FieldType.Float => Float(el.GetSingle()),
        FieldType.Int => Int(el.GetInt32()),
        FieldType.Bool => Bool(el.GetBoolean()),
        FieldType.String => Text(el.GetString() ?? string.Empty),
        FieldType.Dice => FromDice(Dice.FromJsonElement(el)
                                   ?? throw new FormatException("Invalid Dice JSON in FieldValue.")),
        FieldType.Tag => Tag(el.GetInt32()),
        FieldType.Unit => FromUnit(Unit.FromJsonElement(el)
                                   ?? throw new FormatException("Invalid Unit JSON in FieldValue.")),
        FieldType.Reference => Reference(
                                   JsonSerializer.Deserialize<FieldReference>(el.GetRawText())
                                   ?? throw new FormatException("Invalid Reference JSON in FieldValue.")),
        FieldType.Undefined => Undefined,
        _ => throw new FormatException($"Cannot deserialize unknown FieldType: {type}")
    };

    // ─────────────────────────────────────────────────────────────────────────
    // Validation — centralizes the logic scattered across Unit.IsValidForType
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Returns true if <paramref name="raw"/> (a value from a legacy <c>object?</c> dictionary)
    /// is a valid representation of <paramref name="type"/>.
    /// On failure, <paramref name="error"/> contains a human-readable explanation.
    /// </summary>
    public static bool IsValidRawValue(object? raw, FieldType type, out string? error)
    {
        error = null;
        if (raw is null)
        {
            if (type is FieldType.Undefined) return true;
            return Fail(out error, $"null is not valid for FieldType.{type}.");
        }

        return type switch
        {
            FieldType.Float => CheckFloat(raw, out error),
            FieldType.Int => CheckInt(raw, out error),
            FieldType.Bool => CheckBool(raw, out error),
            FieldType.String => true,   // anything can be ToString()'d
            FieldType.Dice => CheckDice(raw, out error),
            FieldType.Tag => CheckTag(raw, out error),
            FieldType.Unit => CheckUnit(raw, out error),
            FieldType.Reference => CheckReference(raw, out error),
            FieldType.Undefined => true,
            _ => Fail(out error, $"Unknown FieldType: {type}")
        };
    }

    private static bool CheckFloat(object raw, out string? error)
    {
        error = null;
        if (raw is sbyte or byte or short or ushort or int or uint or long or ulong or float or double or decimal)
            return true;
        if (raw is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
            return true;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Number)
            return true;
        return Fail(out error, "Expected a numeric value or a parseable numeric string.");
    }

    private static bool CheckInt(object raw, out string? error)
    {
        error = null;
        if (raw is sbyte or byte or short or ushort or int or uint or long or ulong)
            return true;
        if (raw is string s && long.TryParse(s, out _))
            return true;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out _))
            return true;
        return Fail(out error, "Expected an integer value or a parseable integer string.");
    }

    private static bool CheckBool(object raw, out string? error)
    {
        error = null;
        if (raw is bool) return true;
        if (raw is string s && bool.TryParse(s, out _)) return true;
        if (raw is JsonElement je && je.ValueKind is JsonValueKind.True or JsonValueKind.False)
            return true;
        return Fail(out error, "Expected true, false, or a parseable boolean string.");
    }

    private static bool CheckDice(object raw, out string? error)
    {
        error = null;
        if (raw is Dice) return true;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return Dice.FromJsonElement(je) is not null || Fail(out error, "JSON object is not a valid Dice specification.");
        if (raw is string s)
            return Dice.FromJson(s) is not null || Fail(out error, "String is not a valid Dice JSON specification.");
        return Fail(out error, "Expected a Dice object, JSON object, or Dice JSON string.");
    }

    private static bool CheckTag(object raw, out string? error)
    {
        error = null;
        if (raw is int or uint or long or ulong) return true;
        if (raw is string s && int.TryParse(s, out _)) return true;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Number && je.TryGetInt32(out _)) return true;
        return Fail(out error, "Tag value must be an integer ID.");
    }

    private static bool CheckUnit(object raw, out string? error)
    {
        error = null;
        if (raw is Unit) return true;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            if (je.TryGetProperty("TemplateId", out _)) return true;
            return Fail(out error, "Unit JSON must contain a 'TemplateId' property.");
        }
        if (raw is string s)
            return Unit.FromJson(s) is not null || Fail(out error, "String is not valid Unit JSON.");
        return Fail(out error, "Expected a Unit object, JSON object with 'TemplateId', or Unit JSON string.");
    }

    private static bool CheckReference(object raw, out string? error)
    {
        error = null;
        if (raw is FieldReference) return true;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
        {
            if (je.TryGetProperty("UnitId", out _)) return true;
            return Fail(out error, "Reference JSON must contain a 'UnitId' property.");
        }
        return Fail(out error, "Expected a FieldReference or JSON object with 'UnitId'.");
    }

    private static bool Fail(out string? error, string message) { error = message; return false; }

    // ─────────────────────────────────────────────────────────────────────────
    // Parsing from raw object? — replaces Unit.GetValueParsed
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Wraps a raw <c>object?</c> value (from legacy code that uses <c>Dictionary&lt;string, object?&gt;</c>)
    /// into a typed <see cref="FieldValue"/> by interpreting it as <paramref name="targetType"/>.
    /// Returns <see cref="Undefined"/> if the value is null or cannot be parsed.
    /// </summary>
    public static FieldValue Parse(object? raw, FieldType targetType)
    {
        if (raw is null) return Undefined;

        try
        {
            return targetType switch
            {
                FieldType.Float => Float(ParseAsFloat(raw)),
                FieldType.Int => Int(ParseAsInt(raw)),
                FieldType.Bool => Bool(ParseAsBool(raw)),
                FieldType.String => Text(raw.ToString() ?? string.Empty),
                FieldType.Dice => FromDice(ParseAsDice(raw)),
                FieldType.Tag => Tag(ParseAsInt(raw)),
                FieldType.Unit => FromUnit(ParseAsUnit(raw)),
                FieldType.Reference => raw is FieldReference r ? Reference(r) : Undefined,
                _ => Undefined
            };
        }
        catch
        {
            return Undefined;
        }
    }

    private static float ParseAsFloat(object raw)
    {
        if (raw is float f) return f;
        if (raw is double d) return (float)d;
        if (raw is int i) return i;
        if (raw is long l) return l;
        if (raw is string s && float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var pf)) return pf;
        if (raw is JsonElement je && je.TryGetSingle(out var jf)) return jf;
        return Convert.ToSingle(raw, CultureInfo.InvariantCulture);
    }

    private static int ParseAsInt(object raw)
    {
        if (raw is int i) return i;
        if (raw is long l) return (int)l;
        if (raw is double d) return (int)d;
        if (raw is float f) return (int)f;
        if (raw is string s && int.TryParse(s, out var pi)) return pi;
        if (raw is JsonElement je && je.TryGetInt32(out var ji)) return ji;
        return Convert.ToInt32(raw, CultureInfo.InvariantCulture);
    }

    private static bool ParseAsBool(object raw)
    {
        if (raw is bool b) return b;
        if (raw is string s && bool.TryParse(s, out var pb)) return pb;
        if (raw is int i) return i != 0;
        if (raw is JsonElement je)
        {
            if (je.ValueKind == JsonValueKind.True) return true;
            if (je.ValueKind == JsonValueKind.False) return false;
        }
        return Convert.ToBoolean(raw);
    }

    private static Dice ParseAsDice(object raw)
    {
        if (raw is Dice d) return d;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return Dice.FromJsonElement(je) ?? throw new FormatException("Invalid Dice JSON element.");
        if (raw is string s)
            return Dice.FromJson(s) ?? throw new FormatException($"Cannot parse Dice from string: \"{s}\"");
        throw new InvalidCastException($"Cannot convert {raw.GetType().Name} to Dice.");
    }

    private static Unit ParseAsUnit(object raw)
    {
        if (raw is Unit u) return u;
        if (raw is JsonElement je && je.ValueKind == JsonValueKind.Object)
            return Unit.FromJsonElement(je) ?? throw new FormatException("Invalid Unit JSON element.");
        if (raw is string s)
            return Unit.FromJson(s) ?? throw new FormatException($"Cannot parse Unit from string: \"{s}\"");
        throw new InvalidCastException($"Cannot convert {raw.GetType().Name} to Unit.");
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Legacy object? interop — bridge for gradual migration away from object?
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Unwraps this value back to a plain <c>object?</c> for code that still uses
    /// <c>Dictionary&lt;string, object?&gt;</c> (e.g. node Execute / Evaluate delegates).
    /// <list type="bullet">
    ///   <item>Scalars return their inner value directly.</item>
    ///   <item>Arrays return a <c>List&lt;object?&gt;</c> of the unwrapped elements.</item>
    ///   <item><see cref="Undefined"/> returns null.</item>
    /// </list>
    /// </summary>
    public object? ToRawObject() =>
        Type is FieldType.Undefined ? null :
        Rank > 0 ? AsArray().Select(e => e.ToRawObject()).ToList<object?>() :
        (object?)_inner;

    // ─────────────────────────────────────────────────────────────────────────
    // Object overrides
    // ─────────────────────────────────────────────────────────────────────────

    public override string ToString()
    {
        if (Type is FieldType.Undefined) return "undefined";
        if (Rank == 0) return $"{Type}({_inner})";
        var brackets = string.Concat(Enumerable.Repeat("[]", Rank));
        return $"{Type}{brackets}[{string.Join(", ", AsArray())}]";
    }
}
