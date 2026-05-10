using System.ComponentModel.DataAnnotations.Schema;

namespace YggdrasilApi.Models;

/// <summary>
/// Defines a single typed, optionally constrained field on a <see cref="Template"/>.
/// Every <see cref="Unit"/> derived from that template stores one value per declaration.
/// </summary>
public class Decleration
{
    public int Id { get; set; }

    /// <summary>Display name for this field, e.g. "Health", "Effects", or "Attack Bonus".</summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// The base <see cref="FieldType"/> stored in this field.
    /// Use <see cref="FieldValue.Parse"/> to read a stored value and
    /// <see cref="FieldValue.IsValidRawValue"/> to validate one before writing.
    /// </summary>
    public FieldType Type { get; set; } = FieldType.Undefined;

    /// <summary>
    /// Array depth of the value stored in this field — mirrors <see cref="FieldValue.Rank"/>.
    /// <c>0</c> = scalar, <c>1</c> = flat array, <c>2</c> = array of arrays, etc.
    /// </summary>
    public int Rank { get; set; } = 0;

    /// <summary>
    /// The constraint stored as a JSON string in the DB column.
    /// Use the computed <see cref="Constraint"/> property for typed access.
    /// </summary>
    /// <example>
    /// Tag array that only allows children of the "Status Effects" tag:
    /// <code>
    /// declaration.Constraint = new TagParentConstraint { ParentTagId = 5 };
    /// // ConstraintJson is now: {"kind":"tagParent","parentTagId":5}
    /// </code>
    /// </example>
    [Column(TypeName = "nvarchar(max)")]
    public string? ConstraintJson { get; set; }

    /// <summary>
    /// The parsed <see cref="FieldConstraint"/> for this field.
    /// Backed by <see cref="ConstraintJson"/>: reading deserializes, writing serializes.
    /// This property is not mapped to the DB directly.
    /// </summary>
    [NotMapped]
    public FieldConstraint? Constraint
    {
        get => FieldConstraint.Deserialize(ConstraintJson);
        set => ConstraintJson = value?.Serialize();
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a proposed <paramref name="value"/> against this declaration's
    /// <see cref="Type"/>, <see cref="Rank"/>, and <see cref="Constraint"/>.
    /// Returns null on success, or a human-readable error message on failure.
    /// </summary>
    public string? Validate(FieldValue value, IConstraintContext context)
    {
        // Type check — skip when either side is Undefined (accept-any)
        if (Type != FieldType.Undefined && value.Type != FieldType.Undefined)
        {
            if (value.Type != Type && !value.CanConvertTo(Type))
                return $"Declaration '{Name}': expected FieldType.{Type} (Rank {Rank}) " +
                       $"but received {value.Type} (Rank {value.Rank}).";
        }

        // Rank check
        if (value.Rank != Rank)
            return $"Declaration '{Name}': expected Rank {Rank} but received Rank {value.Rank}.";

        // Constraint check (automatically recurses into array elements)
        return Constraint?.Validate(value, context);
    }
}
