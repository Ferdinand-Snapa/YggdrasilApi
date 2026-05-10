using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text.Json;

namespace YggdrasilApi.Models
{
    /// <summary>
    /// Represents an instantiated unit derived from a Template.
    /// Values is a dictionary mapping Declaration.Id -> value for that declaration.
    /// Values should conform to the Decleration.Type for the referenced declaration.
    /// </summary>
    public class Unit
    {
        // The template this unit is based on
        public int TemplateId { get; set; }

        // Key: Decleration.Id from the Template.Declerations list
        // Value: the stored value for that declaration (nullable)
        public Dictionary<int, object?> Values { get; set; } = new Dictionary<int, object?>();

        /// <summary>
        /// Try to set a value for a declaration with runtime validation against the expected type.
        /// Returns true if the value is accepted and stored, false otherwise.
        /// </summary>
        public bool TrySetValue(int declerationId, object? value, FieldType expectedType, out string? error)
        {
            error = null;
            // Delegate validation to the centralised FieldValue system.
            if (!FieldValue.IsValidRawValue(value, expectedType, out var err))
            {
                error = err;
                return false;
            }

            Values[declerationId] = value;
            return true;
        }

        /// <summary>
        /// Get the stored value for a declaration, or null if not present.
        /// </summary>
        public object? GetValue(int declerationId)
            => Values.TryGetValue(declerationId, out var v) ? v : null;

        /// <summary>
        /// Serialize this Unit to JSON format: { "TemplateId": id, "Values": { "1": value, "2": value } }
        /// </summary>
        public string ToJson()
        {
            var dto = new
            {
                TemplateId,
                Values
            };
            return JsonSerializer.Serialize(dto);
        }

        /// <summary>
        /// Deserialize a Unit from JSON format: { "TemplateId": id, "Values": { "declerationId#1": value, "declerationId#2": value } }
        /// Returns null if JSON is invalid.
        /// </summary>
        public static Unit? FromJson(string json)
        {
            try
            {
                using var doc = JsonDocument.Parse(json);
                var root = doc.RootElement;
                if (root.ValueKind != JsonValueKind.Object) return null;

                if (!root.TryGetProperty("TemplateId", out var templateIdEl) || !templateIdEl.TryGetInt32(out var templateId))
                    return null;

                var unit = new Unit { TemplateId = templateId };

                if (root.TryGetProperty("Values", out var valuesEl) && valuesEl.ValueKind == JsonValueKind.Object)
                {
                    foreach (var prop in valuesEl.EnumerateObject())
                    {
                        if (int.TryParse(prop.Name, out var declId))
                        {
                            unit.Values[declId] = prop.Value;
                        }
                    }
                }

                return unit;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Deserialize a Unit from a JsonElement.
        /// </summary>
        public static Unit? FromJsonElement(JsonElement element)
        {
            if (element.ValueKind != JsonValueKind.Object) return null;

            if (!element.TryGetProperty("TemplateId", out var templateIdEl) || !templateIdEl.TryGetInt32(out var templateId))
                return null;

            var unit = new Unit { TemplateId = templateId };

            if (element.TryGetProperty("Values", out var valuesEl) && valuesEl.ValueKind == JsonValueKind.Object)
            {
                foreach (var prop in valuesEl.EnumerateObject())
                {
                    if (int.TryParse(prop.Name, out var declId))
                    {
                        unit.Values[declId] = prop.Value;
                    }
                }
            }

            return unit;
        }

        /// <summary>
        /// Get the stored value for a declaration, parsed and converted to the expected type.
        /// Returns the parsed/converted value or null if not present or unable to parse.
        /// For Dice declarations, returns a Dice object.
        /// For Float declarations, returns a double.
        /// For Bool declarations, returns a bool.
        /// For String declarations, returns a string.
        /// </summary>
        public object? GetValueParsed(int declerationId, FieldType expectedType)
        {
            if (!Values.TryGetValue(declerationId, out var rawValue)) return null;
            if (rawValue is null) return null;

            // Delegate to the centralised FieldValue parsing system.
            var parsed = FieldValue.Parse(rawValue, expectedType);
            return parsed.Type == FieldType.Undefined ? rawValue : parsed.ToRawObject();
        }

        private static bool IsValidForType(object? value, FieldType type, out string? error)
        {
            error = null;
            // Allow null values only for Undefined type
            if (value is null)
            {
                if (type == FieldType.Undefined)
                    return true;
                error = "Value cannot be null for the specified declaration type.";
                return false;
            }

            switch (type)
            {
                case FieldType.Float:
                    // Accept any numeric type or string parseable to number
                    if (value is sbyte || value is byte || value is short || value is ushort || value is int || value is uint || value is long || value is ulong || value is float || value is double || value is decimal)
                        return true;
                    if (value is string s && double.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out _))
                        return true;
                    error = "Value is not a numeric type.";
                    return false;

                case FieldType.String:
                    if (value is string) return true;
                    // allow other types by converting to string implicitly
                    return true;

                case FieldType.Bool:
                    if (value is bool) return true;
                    if (value is string sv && bool.TryParse(sv, out _)) return true;
                    error = "Value is not a boolean.";
                    return false;

                case FieldType.Dice:
                    // Dice declarations are represented as a JSON object mapping number-of-sides -> count
                    // Example: { "6": 2, "20": 1 } means two d6 and one d20.

                    // Validate that keys are positive integers (sides) and values are non-negative integers (counts).
                    {
                        // If value is already a JsonElement (from stored JSON), handle it
                        if (value is System.Text.Json.JsonElement je && je.ValueKind == System.Text.Json.JsonValueKind.Object)
                        {
                            if (!je.EnumerateObject().Any()) { error = "Dice specification must contain at least one entry."; return false; }
                            foreach (var prop in je.EnumerateObject())
                            {
                                if (!int.TryParse(prop.Name, out var sides) || sides <= 0)
                                {
                                    error = $"Invalid dice sides key '{prop.Name}'. Must be a positive integer.";
                                    return false;
                                }

                                var v = prop.Value;
                                int count = 0;
                                if (v.ValueKind == System.Text.Json.JsonValueKind.Number && v.TryGetInt32(out count)) { }
                                else if (v.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(v.GetString(), out count)) { }
                                else { error = $"Invalid dice count for sides '{sides}'. Must be an integer."; return false; }
                            }

                            return true;
                        }

                        // If value is a string, try to parse JSON or allow legacy "1d6" notation
                        if (value is string strVal)
                        {
                            strVal = strVal.Trim();
                            // Try JSON parse first
                            try
                            {
                                using var doc = System.Text.Json.JsonDocument.Parse(strVal);
                                var root = doc.RootElement;
                                if (root.ValueKind == System.Text.Json.JsonValueKind.Object)
                                {
                                    if (!root.EnumerateObject().Any()) { error = "Dice specification must contain at least one entry."; return false; }
                                    foreach (var prop in root.EnumerateObject())
                                    {
                                        if (!int.TryParse(prop.Name, out var sides) || sides <= 0)
                                        {
                                            error = $"Invalid dice sides key '{prop.Name}'. Must be a positive integer.";
                                            return false;
                                        }

                                        int count = 0;
                                        if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.Number && prop.Value.TryGetInt32(out count)) { }
                                        else if (prop.Value.ValueKind == System.Text.Json.JsonValueKind.String && int.TryParse(prop.Value.GetString(), out count)) { }
                                        else { error = $"Invalid dice count for sides '{sides}'. Must be an integer."; return false; }

                                        if (count < 0) { error = "Dice count must be non-negative."; return false; }
                                    }

                                    return true;
                                }
                            }
                            catch (System.Text.Json.JsonException)
                            {
                                error = "Value is not valid JSON. For dice declarations, provide a JSON object like { \"6\":2 } or use the format '2d6'.";
                                return false;
                            }
                        }

                        // If value is a dictionary-like object, try to validate its entries
                        if (value is System.Collections.IDictionary dict)
                        {
                            if (dict.Count == 0) { error = "Dice specification must contain at least one entry."; return false; }
                            foreach (System.Collections.DictionaryEntry de in dict)
                            {
                                var key = de.Key?.ToString();
                                if (!int.TryParse(key, out var sides) || sides <= 0)
                                {
                                    error = $"Invalid dice sides key '{key}'. Must be a positive integer.";
                                    return false;
                                }

                                int count;
                                if (de.Value is int iv) count = iv;
                                else if (de.Value is long lv) count = (int)lv;
                                else if (de.Value is string strVal2 && int.TryParse(strVal2, out var parsedCount)) count = parsedCount;
                                else { error = $"Invalid dice count for sides '{sides}'. Must be an integer."; return false; }

                                if (count < 0) { error = "Dice count must be non-negative."; return false; }
                            }

                            return true;
                        }

                        error = "Value is not a recognized dice specification (expected JSON object like { \"6\":2 }).";
                        return false;
                    }

                case FieldType.Unit:
                    // Unit declarations store a serialized Unit object (JSON with TemplateId and Values)
                    if (value is Unit) return true;
                    if (value is System.Text.Json.JsonElement ue && ue.ValueKind == System.Text.Json.JsonValueKind.Object)
                    {
                        // Validate that it has TemplateId and Values
                        if (ue.TryGetProperty("TemplateId", out var templateIdEl) && templateIdEl.ValueKind == System.Text.Json.JsonValueKind.Number)
                            return true;
                        error = "Unit JSON must contain a 'TemplateId' property.";
                        return false;
                    }
                    if (value is string unitStr)
                    {
                        // Try to parse JSON
                        try
                        {
                            using var doc = System.Text.Json.JsonDocument.Parse(unitStr);
                            var root = doc.RootElement;
                            if (root.ValueKind == System.Text.Json.JsonValueKind.Object &&
                                root.TryGetProperty("TemplateId", out var tid) &&
                                tid.ValueKind == System.Text.Json.JsonValueKind.Number)
                                return true;
                            error = "Unit JSON must be an object with a 'TemplateId' property.";
                            return false;
                        }
                        catch (System.Text.Json.JsonException ex)
                        {
                            error = "Invalid JSON for Unit declaration: " + ex.Message;
                            return false;
                        }
                    }
                    if (value is System.Collections.IDictionary unitDict)
                    {
                        // Check if it has TemplateId key
                        if (unitDict.Contains("TemplateId") || unitDict.Contains(0))
                            return true;
                        error = "Unit object must contain a 'TemplateId' key.";
                        return false;
                    }
                    error = "Unit value must be a JSON object with 'TemplateId' property.";
                    return false;

                case FieldType.Undefined:
                    // accept anything for undefined
                    return true;

                default:
                    error = "Unknown declaration type.";
                    return false;
            }
        }
    }
}
