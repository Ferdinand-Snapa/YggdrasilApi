using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick
{
    // A definition for a node type in the flow scripting system.
    // This is a runtime-only class: it describes ports and behaviour and is not mapped to the database.
    // All properties are init-only — definitions are built once at startup and never mutated.
    public class NodeDefinition
    {
        // Identifier/key for this node type (matches Node.Type)
        public required string Type { get; init; }

        // Port lists — immutable after construction
        public IReadOnlyList<PortDefenition> InputPorts { get; init; } = [];
        public IReadOnlyList<PortDefenition> OutputPorts { get; init; } = [];

        // Execution result returned by Evaluate / Execute.
        // DataOutputs maps output data-port names to produced values.
        // FlowOutputs lists the output flow-port ids that should be triggered next.
        public class NodeExecutionResult
        {
            public Dictionary<string, object?> DataOutputs { get; set; } = new Dictionary<string, object?>();
            public int[] FlowOutputs { get; set; } = [];
        }

        // Evaluate — purely functional nodes; synchronous, no side-effects.
        // (inputs, nodeValues, charId, session) => result
        public Func<
            IDictionary<string, object?>,
            IDictionary<string, object?>,
            int,
            GameSession?,
            Task<NodeExecutionResult?>?>? Evaluate
        { get; init; }

        // Execute — nodes with side-effects or async work.
        // (inputs, nodeValues, charId, session) => Task<result>
        public Func<
            IDictionary<string, object?>,
            IDictionary<string, object?>,
            int,
            GameSession?,
            Task<NodeExecutionResult?>?>? Execute
        { get; init; }
    }
}
