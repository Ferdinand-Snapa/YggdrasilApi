namespace YggdrasilApi.Models;

/// <summary>
/// Every primitive type that can flow through a graph port or be stored on a unit declaration.
/// Used as the discriminator for <see cref="FieldValue"/>.
/// </summary>
public enum FieldType
{
    /// <summary>No type assigned. Treated as "accept anything" on ports, and as null on declarations.</summary>
    Undefined,

    /// <summary>IEEE 754 single-precision floating-point number.</summary>
    Float,

    /// <summary>32-bit signed integer.</summary>
    Int,

    /// <summary>Boolean — true or false.</summary>
    Bool,

    /// <summary>UTF-8 text string.</summary>
    String,

    /// <summary>
    /// A collection of polyhedral dice, e.g. 2d6 + 1d20.
    /// Stored as a <see cref="Dice"/> object.
    /// </summary>
    Dice,

    /// <summary>
    /// A reference to a <see cref="Tag"/> by its integer ID.
    /// Stored as the tag's integer primary key.
    /// </summary>
    Tag,

    /// <summary>
    /// An inline <see cref="Unit"/> instance — a template instantiation with its current field values.
    /// </summary>
    Unit,

    /// <summary>
    /// A lazy pointer to a <see cref="Unit"/> (and optionally a specific declaration field on it)
    /// that already exists inside the active game session.
    /// Stored as a <see cref="FieldReference"/>.
    /// </summary>
    Reference,
}
