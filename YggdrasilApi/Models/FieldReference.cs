namespace YggdrasilApi.Models;

/// <summary>
/// A pointer to a <see cref="Unit"/> that already exists in the active game session,
/// and optionally a specific declaration field on that unit.
/// This is the runtime representation of <see cref="FieldType.Reference"/> values.
/// </summary>
/// <param name="UnitId">The session-scoped ID of the target unit.</param>
/// <param name="DeclarationId">
/// When set, the reference resolves to a specific declaration field on the unit.
/// When null, the reference resolves to the entire unit.
/// </param>
public sealed record FieldReference(int UnitId, int? DeclarationId = null);
