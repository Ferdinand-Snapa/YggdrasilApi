using System;
using System.Text.Json;
using System.Threading.Tasks;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick
{
    // Register built-in node definitions here. Call BuiltinNodeDefinitions.RegisterDefaults() at startup.
    public static class BuiltinNodeDefinitions
    {
        public static void RegisterDefaults()
        {
            // Const node: returns the value stored under Values["ConstValue"]
            var constDef = new NodeDefinition("Const");
            // Const node has an output data port named "Value"
            constDef.OutputPorts.Add(new DataPort { PortId = 0, Name = "Value" });
            constDef.Evaluate = (inputs, nodeValues) =>
            {
                if (!nodeValues.TryGetValue("ConstValue", out var raw))
                    return null;

                var v = UnwrapJsonValue(raw);
                return new NodeDefinition.NodeExecutionResult
                {
                    DataOutputs = new Dictionary<string, object?> { ["Value"] = v },
                    FlowOutputs = Array.Empty<string>()
                };
            };

            NodeRegistry.Register(constDef);

            // Print node: has a Flow input port and a Data input port named "Value".
            var printDef = new NodeDefinition("Print");
            printDef.InputPorts.Add(new FlowPort { PortId = 0, Name = "In" });
            printDef.InputPorts.Add(new DataPort { PortId = 1, Name = "Value", PortType = "any" });
            printDef.OutputPorts.Add(new FlowPort { PortId = 2, Name = "Out" });

            printDef.Execute = async (inputs, nodeValues) =>
            {
                object? value = null;
                if (inputs != null && inputs.TryGetValue("Value", out var inRaw))
                    value = UnwrapJsonValue(inRaw);
                else if (nodeValues != null && nodeValues.TryGetValue("Value", out var nvRaw))
                    value = UnwrapJsonValue(nvRaw);

                Console.WriteLine(value?.ToString() ?? "(null)");

                // After printing, trigger the "Out" flow port if present
                return new NodeDefinition.NodeExecutionResult
                {
                    DataOutputs = new System.Collections.Generic.Dictionary<string, object?>(),
                    FlowOutputs = new[] { "Out" }
                };
            };

            NodeRegistry.Register(printDef);
        }

        private static object? UnwrapJsonValue(object? raw)
        {
            if (raw is null) return null;
            if (raw is JsonElement je)
            {
                switch (je.ValueKind)
                {
                    case JsonValueKind.String: return je.GetString();
                    case JsonValueKind.Number:
                        if (je.TryGetInt64(out var l)) return l;
                        if (je.TryGetDouble(out var d)) return d;
                        return je.GetRawText();
                    case JsonValueKind.True: return true;
                    case JsonValueKind.False: return false;
                    case JsonValueKind.Null: return null;
                    default:
                        // return raw JSON text for objects/arrays
                        return je.GetRawText();
                }
            }

            return raw;
        }
    }
}
