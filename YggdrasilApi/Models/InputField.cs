namespace YggdrasilApi.Models;

/// <summary>
/// Defines a single typed, optionally constrained field that a player is expected
/// to fill in as part of a <c>UserInputRequest</c>.
///
/// <para>
/// Mirrors <see cref="Decleration"/> in intent, but is ephemeral: it lives inside a
/// request schema dictionary rather than being stored on a template.
/// </para>
///
/// <para><b>Example — ask the player to choose a status-effect tag:</b></para>
/// <code>
/// new InputField
/// {
///     Label      = "Apply Effect",
///     Type       = FieldType.Tag,
///     Rank       = 0,   // single tag
///     Required   = true,
///     Constraint = new TagParentConstraint { ParentTagId = statusEffectsTagId }
/// }
/// </code>
///
/// <para><b>Example — ask the player to choose a target unit that is a Warrior:</b></para>
/// <code>
/// new InputField
/// {
///     Label      = "Target",
///     Type       = FieldType.Reference,
///     Constraint = new UnitTemplateConstraint { AllowedTemplateIds = [warriorTemplateId] }
/// }
/// </code>
/// </summary>
public sealed record InputField
{
    /// <summary>Short label shown to the player, e.g. "Target Unit" or "Damage Amount".</summary>
    public string Label { get; init; } = string.Empty;

    /// <summary>Optional longer description or hint for the player.</summary>
    public string? Description { get; init; }

    /// <summary>
    /// The expected base <see cref="FieldType"/> of the response value.
    /// <see cref="FieldType.Undefined"/> means "accept any type".
    /// </summary>
    public FieldType Type { get; init; } = FieldType.Undefined;

    /// <summary>
    /// Array depth of the expected value — mirrors <see cref="FieldValue.Rank"/>.
    /// <c>0</c> = single scalar; <c>1</c> = flat list; <c>2</c> = list of lists, etc.
    /// </summary>
    public int Rank { get; init; } = 0;

    /// <summary>
    /// Optional dictionary specifying the exact number of elements expected at each array depth.
    /// <list type="bullet">
    ///   <item>Key = depth level: <c>1</c> is the outermost array, <c>2</c> is each inner array, etc.</item>
    ///   <item>Value = required count. Use <c>-1</c> to allow any number of elements at that depth.</item>
    /// </list>
    /// Depths that are absent from the dictionary are unconstrained.
    /// </summary>
    /// <example>
    /// Exactly 2 d6 results (Rank 1):
    /// <code>ArrayCount = new() { [1] = 2 }</code>
    ///
    /// A 2-D jagged array where the outer list is free-length
    /// but every inner list must have exactly 3 elements:
    /// <code>ArrayCount = new() { [1] = -1, [2] = 3 }</code>
    ///
    /// Partial rolling allowed (any number of d20s, up to the server to fill the rest):
    /// <code>ArrayCount = new() { [1] = -1 }</code>
    /// </example>
    public Dictionary<int, int> ArrayCount { get; init; } = [];


    /// <summary>
    /// Optional constraint that further restricts which values are accepted.
    /// When null, any value matching <see cref="Type"/> and <see cref="Rank"/> is valid.
    /// </summary>
    public FieldConstraint? Constraint { get; init; }

    /// <summary>
    /// When true (default), the player must provide a value for this field.
    /// When false, the field may be omitted from the response.
    /// </summary>
    public bool Required { get; init; } = true;

    // ─────────────────────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a proposed response <paramref name="value"/> against this field's
    /// type, rank, array counts, and constraint.
    /// Returns null on success, or a human-readable error message on failure.
    /// </summary>
    public string? Validate(FieldValue value, IConstraintContext context)
    {
        // Type check — skip when either side is Undefined (accept-any)
        if (Type != FieldType.Undefined && value.Type != FieldType.Undefined)
        {
            if (value.Type != Type && !value.CanConvertTo(Type))
                return $"Expected FieldType.{Type} (Rank {Rank}) " +
                       $"but received {value.Type} (Rank {value.Rank}).";
        }

        // Rank check
        if (value.Rank != Rank)
            return $"Expected Rank {Rank} but received Rank {value.Rank}.";

        // ArrayCount check — verifies element counts at each array depth level.
        // Only runs when ArrayCount has entries and the value is actually an array.
        if (ArrayCount.Count > 0 && value.IsArray)
        {
            var countError = CheckArrayCount(value, depth: 1);
            if (countError is not null) return countError;
        }

        // Constraint check (automatically recurses into arrays)
        return Constraint?.Validate(value, context);
    }

    // ─────────────────────────────────────────────────────────────────────────
    // ArrayCount helper
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Recursively validates element counts at each array depth starting at
    /// <paramref name="depth"/> = 1 for the outermost array.
    /// </summary>
    private string? CheckArrayCount(FieldValue value, int depth)
    {
        int actualCount = value.AsArray().Count;

        // Enforce the count at this depth if a requirement is defined.
        // -1 is the "any count" sentinel — skip the check in that case.
        if (ArrayCount.TryGetValue(depth, out int required) && required >= 0)
        {
            if (actualCount != required)
                return $"Expected exactly {required} element(s) at array depth {depth} " +
                       $"for '{Label}', but found {actualCount}.";
        }

        // Recurse into nested arrays.
        // All elements share the same Rank, so we only need to check whether the
        // first element is itself an array to decide whether to go deeper.
        foreach (var element in value.AsArray())
        {
            if (!element.IsArray) break; // reached scalar elements — no deeper arrays exist

            var error = CheckArrayCount(element, depth + 1);
            if (error is not null) return error;
        }

        return null;
    }
}
