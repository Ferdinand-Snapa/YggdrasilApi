using System;
using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json;
using YggdrasilApi.GameLogick;

namespace YggdrasilApi.Models
{
    public class Node
    {
        /// <summary>
        /// Unique identifier for this node. A new GUID is automatically assigned on construction.
        /// Marked as never database-generated so EF uses the value we set in C#.
        /// </summary>
        [DatabaseGenerated(DatabaseGeneratedOption.None)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public string Type { get; set; } = string.Empty;

        // Persisted JSON blob containing key/value pairs (string -> serialized value).
        // Keeping a single JSON column avoids EF trying to map Dictionary<> as a navigation.
        public string ValuesJson { get; set; } = "{}";

        // Runtime view of the values. Not mapped by EF. Use LoadValues / SaveValues to sync with ValuesJson.
        [NotMapped]
        public Dictionary<string, object?> Values { get; set; } = [];

        public void LoadValues()
        {
            try
            {
                if (string.IsNullOrWhiteSpace(ValuesJson))
                {
                    Values = [];
                    return;
                }

                var doc = JsonSerializer.Deserialize<Dictionary<string, JsonElement?>>(ValuesJson);
                Values = doc?.ToDictionary(k => k.Key, v => (object?)v.Value) ?? [];
            }
            catch
            {
                Values = [];
            }
        }

        public void SaveValues()
        {
            try
            {
                ValuesJson = JsonSerializer.Serialize(Values ?? []);
            }
            catch
            {
                ValuesJson = "{}";
            }
        }

        // Runtime-only pointer to the NodeDefinition matching this node's Type.
        // Not mapped by EF and resolved from the NodeRegistry when accessed.
        [NotMapped]
        public NodeDefinition? Definition
        {
            get
            {
                if (string.IsNullOrEmpty(Type))
                    return null;

                return NodeRegistry.TryGet(Type, out var def) ? def : null;
            }
        }
    }
}
