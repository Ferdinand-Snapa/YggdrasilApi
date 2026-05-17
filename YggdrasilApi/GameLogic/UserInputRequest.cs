using System;
using System.Collections.Generic;
using System.Linq;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

/// <summary>
/// Represents a request for typed user input tied to a specific unit.
/// The controlling player of that unit receives the request and is expected to
/// provide one value per field defined in <see cref="Schema"/>.
/// </summary>
public class UserInputRequest
{
    // ─────────────────────────────────────────────────────────────────────────
    // Identity
    // ─────────────────────────────────────────────────────────────────────────

    public string Id { get; set; }

    /// <summary>The session-scoped ID of the unit this request is tied to.</summary>
    public int UnitId { get; set; }

    /// <summary>
    /// A discriminator string identifying what kind of action is being requested
    /// (e.g. <c>"DiceRoll"</c>, <c>"TargetSelection"</c>, <c>"SpellChoice"</c>).
    /// </summary>


    // ─────────────────────────────────────────────────────────────────────────
    // Schema
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Describes every value the player is expected to provide.
    /// Key = field name (e.g. <c>"Target"</c>, <c>"Amount"</c>);
    /// Value = an <see cref="InputField"/> specifying the type, rank, and optional constraint.
    /// </summary>
    public Dictionary<string, InputField> Schema { get; set; } = new();

    // ─────────────────────────────────────────────────────────────────────────
    // Response
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// The raw response object as received from the API / SignalR hub.
    /// Populated by <see cref="Resolve(object?, string?)"/> for backward compatibility
    /// with code that has not yet been migrated to the typed response path.
    /// </summary>
    public object? RawResponse { get; set; }

    /// <summary>
    /// The validated, typed response values, keyed by the same field names as <see cref="Schema"/>.
    /// Populated by <see cref="ValidateAndResolve"/> after successful validation.
    /// </summary>
    public Dictionary<string, FieldValue> TypedResponse { get; set; } = new();

    // ─────────────────────────────────────────────────────────────────────────
    // State
    // ─────────────────────────────────────────────────────────────────────────

    public bool IsResolved { get; set; }
    public string? ResolvedByPlayerId { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }

    /// <summary>Time elapsed since the request was created.</summary>
    public TimeSpan ElapsedTime => DateTime.UtcNow - CreatedAt;

    // ─────────────────────────────────────────────────────────────────────────
    // Constructors
    // ─────────────────────────────────────────────────────────────────────────

    public UserInputRequest(string id, int unitId,
                            Dictionary<string, InputField> schema)
    {
        Id = id ?? throw new ArgumentNullException(nameof(id));
        Schema = schema ?? new Dictionary<string, InputField>();
        UnitId = unitId;
        CreatedAt = DateTime.UtcNow;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Validation
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates a proposed <paramref name="response"/> dictionary against every
    /// field defined in <see cref="Schema"/>.
    ///
    /// <para>Returns an empty dictionary when the response is fully valid.</para>
    /// <para>Returns one error entry per invalid or missing field when validation fails.</para>
    /// </summary>
    public Dictionary<string, string> ValidateResponse(
        Dictionary<string, FieldValue> response,
        IConstraintContext context)
    {
        var errors = new Dictionary<string, string>();

        // Check that all required schema fields are present and valid.
        foreach (var (fieldName, fieldDef) in Schema)
        {
            if (!response.TryGetValue(fieldName, out var value))
            {
                if (fieldDef.Required)
                    errors[fieldName] = $"Field '{fieldName}' is required but was not provided.";
                continue;
            }

            var error = fieldDef.Validate(value, context);
            if (error is not null)
                errors[fieldName] = error;
        }

        // Flag unexpected fields that the schema does not define.
        foreach (var key in response.Keys.Where(k => !Schema.ContainsKey(k)))
            errors[key] = $"Field '{key}' is not part of the request schema.";

        return errors;
    }

    // ─────────────────────────────────────────────────────────────────────────
    // Resolution
    // ─────────────────────────────────────────────────────────────────────────

    /// <summary>
    /// Validates and resolves this request with a fully typed response.
    /// Returns an empty error dictionary on success, or one entry per invalid field.
    /// The request is only marked as resolved when the dictionary is empty.
    /// </summary>
    public Dictionary<string, string> ValidateAndResolve(
        Dictionary<string, FieldValue> response,
        IConstraintContext context,
        string? resolvedByPlayerId = null)
    {
        var errors = ValidateResponse(response, context);
        if (errors.Count == 0)
        {
            TypedResponse = response;
            IsResolved = true;
            ResolvedAt = DateTime.UtcNow;
            ResolvedByPlayerId = resolvedByPlayerId;
        }
        return errors;
    }

    /// <summary>
    /// Resolves this request with a raw <c>object?</c> response.
    /// Intended for backward-compatible code that has not yet been migrated
    /// to the typed <see cref="ValidateAndResolve"/> path.
    /// No schema validation is performed.
    /// </summary>
    public void Resolve(object? rawResponse, string? resolvedByPlayerId = null)
    {
        RawResponse = rawResponse;
        IsResolved = true;
        ResolvedAt = DateTime.UtcNow;
        ResolvedByPlayerId = resolvedByPlayerId;
    }
}
