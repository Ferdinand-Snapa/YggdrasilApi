using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace YggdrasilApi.Models;

// ─────────────────────────────────────────────────────────────────────────────
// Constraint context
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Provides the environment lookups that constraints need to perform contextual
/// validation (tag hierarchies, template inheritance, live session units).
///
/// <para>
/// Implement this interface in your service or session layer and pass it to
/// <see cref="FieldConstraint.Validate"/> or <see cref="Decleration.Validate"/>.
/// </para>
/// </summary>
public interface IConstraintContext
{
    /// <summary>
    /// Returns true if the tag identified by <paramref name="tagId"/> is a direct
    /// or indirect descendant of the tag identified by <paramref name="ancestorId"/>
    /// in the realm's tag tree.
    /// </summary>
    bool IsTagDescendant(int tagId, int ancestorId);

    /// <summary>
    /// Returns true if the template identified by <paramref name="templateId"/>
    /// derives from (directly or transitively via <see cref="Template.Derives"/>)
    /// the template identified by <paramref name="requiredTemplateId"/>.
    /// </summary>
    bool TemplateDerivesFrom(int templateId, int requiredTemplateId);

    /// <summary>
    /// Returns the <see cref="Template.Id"/> of the live session unit identified by
    /// <paramref name="sessionUnitId"/>, or null if no such unit exists in the session.
    /// </summary>
    int? GetUnitTemplateId(int sessionUnitId);
}

// ─────────────────────────────────────────────────────────────────────────────
// Base class
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// A type-safe constraint that restricts the values accepted by a
/// <see cref="Decleration"/> field or an <see cref="InputField"/> in a user-input
/// request schema.
///
/// <para>
/// Constraints are defined against individual scalars but automatically applied
/// to every element of an array value (<see cref="FieldValue.Rank"/> &gt; 0).
/// </para>
///
/// <para><b>Built-in constraint types:</b></para>
/// <list type="bullet">
///   <item><see cref="NumericRangeConstraint"/> — clamps <see cref="FieldType.Int"/> /
///     <see cref="FieldType.Float"/> values to [<c>Min</c>, <c>Max</c>].</item>
///   <item><see cref="TagParentConstraint"/> — requires a <see cref="FieldType.Tag"/>
///     to be a descendant of a specific parent tag.</item>
///   <item><see cref="UnitTemplateConstraint"/> — requires a <see cref="FieldType.Unit"/>
///     or <see cref="FieldType.Reference"/> to derive from one of the listed templates.</item>
/// </list>
///
/// <para>Persist with <see cref="Serialize"/> / <see cref="Deserialize"/>
/// (stored as a compact JSON string).</para>
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "kind")]
[JsonDerivedType(typeof(NumericRangeConstraint), "numericRange")]
[JsonDerivedType(typeof(TagParentConstraint), "tagParent")]
[JsonDerivedType(typeof(UnitTemplateConstraint), "unitTemplate")]
public abstract class FieldConstraint
{
    // ─────────────────────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a single scalar (<see cref="FieldValue.Rank"/> = 0) value.
    /// Returns null on success, or a human-readable error message on failure.
    /// </summary>
    protected abstract string? ValidateScalar(FieldValue value, IConstraintContext context);

    /// <summary>
    /// Validates <paramref name="value"/>, which may be a scalar or a nested array
    /// of any depth. For arrays, <see cref="ValidateScalar"/> is applied to every
    /// element recursively.
    /// Returns null on success, or the first error encountered.
    /// </summary>
    public string? Validate(FieldValue value, IConstraintContext context)
    {
        if (value.Rank == 0)
            return ValidateScalar(value, context);

        foreach (var element in value.AsArray())
        {
            var error = Validate(element, context);
            if (error is not null) return error;
        }

        return null;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // DB / wire serialization
    // ─────────────────────────────────────────────────────────────────────────

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serializes this constraint to a compact JSON string for DB storage.</summary>
    public string Serialize()
        => JsonSerializer.Serialize(this, typeof(FieldConstraint), SerializerOptions);

    /// <summary>
    /// Deserializes a constraint from a JSON string produced by <see cref="Serialize"/>.
    /// Returns null for null or empty input.
    /// </summary>
    public static FieldConstraint? Deserialize(string? json)
    {
        if (string.IsNullOrWhiteSpace(json)) return null;
        return JsonSerializer.Deserialize<FieldConstraint>(json, SerializerOptions);
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// NumericRangeConstraint
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Restricts an <see cref="FieldType.Int"/> or <see cref="FieldType.Float"/> value
/// to an inclusive numeric range [<see cref="Min"/>, <see cref="Max"/>].
/// Either bound may be omitted (null) to leave that side unbounded.
/// </summary>
/// <example>
/// Health that must stay between 0 and 100:
/// <code>new NumericRangeConstraint { Min = 0, Max = 100 }</code>
/// Attack bonus with no upper limit:
/// <code>new NumericRangeConstraint { Min = -10 }</code>
/// </example>
public sealed class NumericRangeConstraint : FieldConstraint
{
    /// <summary>Inclusive lower bound. Null means no lower limit.</summary>
    public float? Min { get; init; }

    /// <summary>Inclusive upper bound. Null means no upper limit.</summary>
    public float? Max { get; init; }

    protected override string? ValidateScalar(FieldValue value, IConstraintContext context)
    {
        float number;
        if (value.Type == FieldType.Float) number = value.AsFloat();
        else if (value.Type == FieldType.Int) number = value.AsInt();
        else return $"NumericRange constraint does not apply to FieldType.{value.Type}.";

        if (Min.HasValue && number < Min.Value)
            return $"Value {number} is below the minimum of {Min.Value}.";
        if (Max.HasValue && number > Max.Value)
            return $"Value {number} exceeds the maximum of {Max.Value}.";

        return null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// TagParentConstraint
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Restricts a <see cref="FieldType.Tag"/> value to tags that are direct or indirect
/// descendants of the tag identified by <see cref="ParentTagId"/>.
/// When the declaration or field has <c>Rank &gt; 0</c> (an array of tags),
/// every element in the array must satisfy this constraint.
/// </summary>
/// <example>
/// Declare an "Effects" field that only allows children of the "Status Effects" tag:
/// <code>
/// new Decleration
/// {
///     Name       = "Effects",
///     Type       = FieldType.Tag,
///     Rank       = 1,   // Tag[]
///     Constraint = new TagParentConstraint { ParentTagId = statusEffectsTagId }
/// }
/// </code>
/// </example>
public sealed class TagParentConstraint : FieldConstraint
{
    /// <summary>
    /// The ID of the required ancestor tag.
    /// Every submitted tag must be a descendant of this tag (as determined by
    /// <see cref="IConstraintContext.IsTagDescendant"/>).
    /// </summary>
    public int ParentTagId { get; init; }

    protected override string? ValidateScalar(FieldValue value, IConstraintContext context)
    {
        if (value.Type != FieldType.Tag)
            return $"TagParent constraint does not apply to FieldType.{value.Type}.";

        int tagId = value.AsTagId();
        if (!context.IsTagDescendant(tagId, ParentTagId))
            return $"Tag {tagId} is not a descendant of the required parent tag {ParentTagId}.";

        return null;
    }
}

// ─────────────────────────────────────────────────────────────────────────────
// UnitTemplateConstraint
// ─────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Restricts a <see cref="FieldType.Unit"/> or <see cref="FieldType.Reference"/> value
/// to units whose template derives from at least one of the <see cref="AllowedTemplateIds"/>.
/// Derivation is checked transitively through <see cref="Template.Derives"/>.
/// </summary>
/// <example>
/// Require a unit to be a "Warrior" (or any sub-template of it):
/// <code>
/// new UnitTemplateConstraint { AllowedTemplateIds = [warriorTemplateId] }
/// </code>
/// Accept both "Warrior" and "Mage" sub-templates:
/// <code>
/// new UnitTemplateConstraint { AllowedTemplateIds = [warriorId, mageId] }
/// </code>
/// </example>
public sealed class UnitTemplateConstraint : FieldConstraint
{
    /// <summary>
    /// The unit must derive from at least one of these template IDs.
    /// Derivation is checked transitively via <see cref="IConstraintContext.TemplateDerivesFrom"/>.
    /// </summary>
    public int[] AllowedTemplateIds { get; init; } = [];

    protected override string? ValidateScalar(FieldValue value, IConstraintContext context)
    {
        int templateId;

        if (value.Type == FieldType.Unit)
        {
            templateId = value.AsUnit().TemplateId;
        }
        else if (value.Type == FieldType.Reference)
        {
            var unitId = value.AsReference().UnitId;
            var templateID = context.GetUnitTemplateId(unitId);
            if (templateID is null)
                return $"Referenced unit {unitId} was not found in the session.";
            templateId = templateID.Value;
        }
        else
        {
            return $"UnitTemplate constraint does not apply to FieldType.{value.Type}.";
        }

        if (!AllowedTemplateIds.Any(id => context.TemplateDerivesFrom(templateId, id)))
        {
            var allowed = string.Join(", ", AllowedTemplateIds);
            return $"Unit with template {templateId} does not derive from any of the required " +
                   $"templates: [{allowed}].";
        }

        return null;
    }
}
