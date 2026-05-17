using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text.Json;
using YggdrasilApi.Utils;

namespace YggdrasilApi.Models
{
    public class Graph
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public List<Node> Nodes { get; set; } = new List<Node>();
        public List<Connection> Connections { get; set; } = new List<Connection>();

        // ── Derived ports ──────────────────────────────────────────────────────────────
        //
        // Instead of storing ports explicitly, the graph infers them from the roles of
        // its nodes.  Four node types participate:
        //
        //   "InputFlow"   → FlowPort  added to InputPorts
        //   "InputValue"  → DataPort  added to InputPorts
        //   "OutputFlow"  → FlowPort  added to OutputPorts
        //   "OutputValue" → DataPort  added to OutputPorts
        //
        // For every such node the PortId equals the node's Id, and the Name / PortType
        // are read from the node's ValuesJson  ("Name" key, "PortType" key).
        // If those keys are absent the node Id is used as the name and "any" as the type.

        /// <summary>
        /// Flow and value input ports inferred from "InputFlow" and "InputValue" nodes.
        /// </summary>
        [NotMapped]
        public List<PortDefenition> InputPorts =>
            [.. Nodes
                .Where(n => n.Type is "InputFlow" or "InputValue")
                .Select(n =>
                {
                    var (name, fieldType) = ReadPortConfig(n);
                    return n.Type == "InputFlow"
                        ? (PortDefenition)new FlowPort { PortId = n.Id, Name = name }
                        : new DataPort  { PortId = n.Id, Name = name, PortType = fieldType };
                })];

        /// <summary>
        /// Flow and value output ports inferred from "OutputFlow" and "OutputValue" nodes.
        /// </summary>
        [NotMapped]
        public List<PortDefenition> OutputPorts =>
            [.. Nodes
                .Where(n => n.Type is "OutputFlow" or "OutputValue")
                .Select(n =>
                {
                    var (name, fieldType) = ReadPortConfig(n);
                    return n.Type == "OutputFlow"
                        ? (PortDefenition)new FlowPort { PortId = n.Id, Name = name }
                        : new DataPort  { PortId = n.Id, Name = name, PortType = fieldType };
                })];

        // ── Helper ─────────────────────────────────────────────────────────────────────

        /// <summary>
        /// Reads "Name" and "PortType" from a node's ValuesJson without mutating the node.
        /// Falls back to the node's Id (as string) for the name and
        /// <see cref="FieldType.Undefined"/> (accept-any) for the port type.
        /// </summary>
        private static (string name, FieldType portType) ReadPortConfig(Node node)
        {
            try
            {
                if (!string.IsNullOrWhiteSpace(node.ValuesJson))
                {
                    using var doc = JsonDocument.Parse(node.ValuesJson);
                    var root = doc.RootElement;

                    var name = root.TryGetProperty(nameof(Name), out var nameProp)
                        ? nameProp.GetString() ?? node.Id
                        : node.Id;

                    // Read the stored string and convert to FieldType via the compatibility helper.
                    // Unrecognised strings resolve to FieldType.Undefined (accept-any).
                    var portTypeStr = root.TryGetProperty("PortType", out var typeProp)
                        ? typeProp.GetString() ?? string.Empty
                        : string.Empty;

                    var portType = TypeCompatibility.ToFieldType(portTypeStr);
                    return (name, portType);
                }
            }
            catch { /* malformed JSON — fall through to defaults */ }

            return (node.Id, FieldType.Undefined);
        }
    }
}
