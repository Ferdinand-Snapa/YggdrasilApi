using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick
{
    // A definition for a node type in the flow scripting system.
    // This is a runtime-only class: it describes ports and behaviour and is not mapped to the database.
    public class NodeDefinition
    {
        // Identifier/key for this node type (matches Node.Type)
        public string Type { get; init; } = string.Empty;

        // Data ports describe value inputs/outputs
        public List<YggdrasilApi.Models.PortDefenition> InputPorts { get; } = new List<YggdrasilApi.Models.PortDefenition>();
        public List<YggdrasilApi.Models.PortDefenition> OutputPorts { get; } = new List<YggdrasilApi.Models.PortDefenition>();

        // Execution result: DataOutputs maps output data port names to values
        // FlowOutputs is an ordered list of output flow port names representing which flows to trigger next
        public class NodeExecutionResult
        {
            public Dictionary<string, object?> DataOutputs { get; set; } = new Dictionary<string, object?>();
            public string[] FlowOutputs { get; set; } = Array.Empty<string>();
        }

        // Evaluate is used for purely functional nodes that return outputs synchronously.
        // Signature: (inputs, nodeValues) => NodeExecutionResult
        // - inputs: dictionary mapping input port names to values (from connected nodes)
        // - nodeValues: the node.Values dictionary (persistent data for the node)
        public Func<IDictionary<string, object?>, IDictionary<string, object?>, NodeExecutionResult?>? Evaluate { get; set; }

        // Execute is used for nodes that perform actions / have side-effects and may be async.
        // Signature: (inputs, nodeValues) => Task<NodeExecutionResult>
        public Func<IDictionary<string, object?>, IDictionary<string, object?>, Task<NodeExecutionResult?>?>? Execute { get; set; }

        public NodeDefinition(string type)
        {
            Type = type;
        }
    }
}
