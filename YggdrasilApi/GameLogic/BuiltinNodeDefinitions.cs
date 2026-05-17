using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick
{
    // All built-in node definitions live here as a static, compile-time dictionary.
    // Call BuiltinNodeDefinitions.RegisterDefaults() once at startup to load them into NodeRegistry.
    public static class BuiltinNodeDefinitions
    {
        public static readonly IReadOnlyDictionary<string, NodeDefinition> All =
            new Dictionary<string, NodeDefinition>
            {
                // ── Start ──────────────────────────────────────────────────────────────
                // Start point for a graph execution.
                // Outputs a single flow output port.
                ["InputFlow"] = new NodeDefinition
                {
                    Type = "InputFlow",
                    OutputPorts =
                    [
                        new FlowPort { PortId = "flow", Name = "Flow" }
                    ],
                    Execute = async (_, _, _, _) =>
                    {
                        return new NodeDefinition.NodeExecutionResult
                        {
                            FlowOutputs = ["flow"]
                        };
                    }
                },
                // ── Const ──────────────────────────────────────────────────────────────
                // Reads a constant value stored in the node's "ConstValue" data field
                // and emits it on the "Value" output port.
                ["Const"] = new NodeDefinition
                {
                    Type = "Const",
                    OutputPorts =
                    [
                        new DataPort { PortId = "value", Name = "Value", PortType = FieldType.Undefined }
                    ],
                    Evaluate = async (inputs, nodeValues, charId, session) =>
                    {
                        if (!nodeValues.TryGetValue("ConstValue", out var raw))
                            return null;

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Value"] = UnwrapJsonValue(raw) },
                            FlowOutputs = []
                        };
                    }
                },

                // ── Input ──────────────────────────────────────────────────────────────
                // Reads the input of the graph and emits it as a value on the "Value" output port.
                ["InputValue"] = new NodeDefinition
                {
                    Type = "Input",
                    OutputPorts =
                    [
                        new DataPort { PortId = "value", Name = "Value", PortType = FieldType.Undefined }
                    ],
                    Evaluate = async (_, _, _, _) =>
                    {
                        return new NodeDefinition.NodeExecutionResult
                        {   // TODO: fix to work with run graph and nodes representing graphs
                            DataOutputs = new Dictionary<string, object?> { ["Value"] = 0 }
                        };
                    }
                },
                // ── Output Flow ───────────────────────────────────────────────────────
                // Emits a flow value that exits the graph into another graph.
                // TODO: Fix to work with run graph and nodes representing graphs
                ["OutputFlow"] = new NodeDefinition
                {
                    Type = "OutputFlow",
                    InputPorts =
                    [
                        new FlowPort { PortId = "out_flow", Name = "OutFlow" }
                    ],
                    Evaluate = async (inputs, _, _, _) =>
                    {
                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Value"] = inputs["Value"] }
                        };
                    }
                },
                // ── Output Data ───────────────────────────────────────────────────────
                // Recieves a value that can be read by other graphs beeing called.
                ["OutputData"] = new NodeDefinition
                {
                    Type = "Output",
                    InputPorts =
                    [
                        new DataPort { PortId = "value", Name = "Value", PortType = FieldType.Undefined }
                    ],
                    Evaluate = async (inputs, _, _, _) =>
                    {
                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Value"] = inputs.TryGetValue("Value", out var v) ? v : null }
                        };
                    }
                },
                // Reads the input of the graph and emits it as a value on the "Value" output port.                // ── Print ──────────────────────────────────────────────────────────────
                // Prints "Value" to the console then continues execution on "Out".
                ["Print"] = new NodeDefinition
                {
                    Type = "Print",
                    InputPorts =
                    [
                        new FlowPort { PortId = "trigger", Name = "In"    },
                        new DataPort  { PortId = "value",   Name = "Value", PortType = FieldType.Undefined }
                    ],
                    OutputPorts =
                    [
                        new FlowPort { PortId = "done", Name = "Out" }
                    ],
                    Execute = async (inputs, nodeValues, charId, session) =>
                    {
                        var value = inputs.TryGetValue("Value", out var inRaw) ? UnwrapJsonValue(inRaw)
                                  : nodeValues.TryGetValue("Value", out var nvRaw) ? UnwrapJsonValue(nvRaw)
                                  : null;

                        Console.WriteLine(value?.ToString() ?? "(null)");
                        await Task.CompletedTask;

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?>(),
                            FlowOutputs = ["done"]
                        };
                    }
                },

                // ── Addition ───────────────────────────────────────────────────────────
                // Adds two numbers and emits the result.
                // Inputs:  A (number), B (number)
                // Outputs: Result (number)
                ["Addition"] = new NodeDefinition
                {
                    Type = "Addition",
                    InputPorts =
                    [
                        new DataPort { PortId = "a", Name = "A", PortType = FieldType.Float },
                        new DataPort { PortId = "b", Name = "B", PortType = FieldType.Float }
                    ],
                    OutputPorts =
                    [
                        new DataPort { PortId = "result", Name = "Result", PortType = FieldType.Float }
                    ],
                    Evaluate = async (inputs, _, _, _) =>
                    {
                        var a = Convert.ToDouble(UnwrapJsonValue(inputs["A"]));
                        var b = Convert.ToDouble(UnwrapJsonValue(inputs["B"]));

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Result"] = a + b },
                            FlowOutputs = []
                        };
                    }
                },

                // ── Multiply ───────────────────────────────────────────────────────────
                // Multiply two numbers and evaluate the result.
                // Inputs: A (number), B (number)
                // Outputs: Result (number)
                ["Multiply"] = new NodeDefinition
                {
                    Type = "Multiply",
                    InputPorts =
                    [
                        new DataPort { PortId = "a", Name = "A", PortType = FieldType.Float },
                        new DataPort { PortId = "b", Name = "B", PortType = FieldType.Float }
                    ],
                    OutputPorts =
                    [
                        new DataPort { PortId = "result", Name = "Result", PortType = FieldType.Float }
                    ],
                    Evaluate = async (inputs, _, _, _) =>
                    {
                        var a = Convert.ToDouble(UnwrapJsonValue(inputs["A"]));
                        var b = Convert.ToDouble(UnwrapJsonValue(inputs["B"]));

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Result"] = a * b },
                            FlowOutputs = []
                        };
                    }
                },

                // ── If ─────────────────────────────────────────────────────────────────
                // Branches on a bool condition.
                // Inputs:  In (flow), Condition (bool)
                // Outputs: True (flow), False (flow)
                ["If"] = new NodeDefinition
                {
                    Type = "If",
                    InputPorts =
                    [
                        new FlowPort { PortId = "trigger",   Name = "In"        },
                        new DataPort  { PortId = "condition", Name = "Condition", PortType = FieldType.Bool }
                    ],
                    OutputPorts =
                    [
                        new FlowPort { PortId = "true",  Name = "True"  },
                        new FlowPort { PortId = "false", Name = "False" }
                    ],
                    Evaluate = async (inputs, _, _, _) =>
                    {
                        var raw = inputs.TryGetValue("Condition", out var r) ? UnwrapJsonValue(r) : null;
                        var condition = raw is true;

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?>(),
                            FlowOutputs = condition ? ["true"] : ["false"]
                        };
                    }
                },

                // ── GetSelfValue ───────────────────────────────────────────────────────
                // Reads a declaration value from the calling unit (identified by charId).
                //
                // Node values (configured per node instance):
                //   "TemplateId"    – int  the template the unit must belong to
                //   "DeclarationId" – int  which declaration to read
                //
                // Outputs: Value (any)  the stored value, parsed to the declaration's type
                ["GetSelfValue"] = new NodeDefinition
                {
                    Type = "GetSelfValue",
                    OutputPorts =
                    [
                        new DataPort { PortId = "value", Name = "Value" }
                    ],
                    Evaluate = async (inputs, nodeValues, charId, session) =>
                    {
                        if (session == null)
                            throw new Exception("GetSelfValue requires a GameSession");

                        var templateId = GetIntNodeValue(nodeValues, "TemplateId");
                        var declId = GetIntNodeValue(nodeValues, "DeclarationId");

                        var unit = GetUnit(session, charId);

                        if (unit.TemplateId != templateId)
                            throw new Exception(
                                $"GetSelfValue: unit {charId} belongs to template {unit.TemplateId}, " +
                                $"but node is configured for template {templateId}");

                        var decl = GetDeclaration(session, templateId, declId);
                        var value = unit.GetValueParsed(declId, decl.Type);

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Value"] = value },
                            FlowOutputs = []
                        };
                    }
                },

                // ── GetUnitValue ───────────────────────────────────────────────────────
                // Reads a declaration value from an arbitrary unit supplied via the "Unit" input port.
                //
                // Node values (configured per node instance):
                //   "TemplateId"    – int  the template the target unit must belong to
                //   "DeclarationId" – int  which declaration to read
                //
                // Inputs:  Unit (unit)  the id of the unit to read from
                // Outputs: Value (any)  the stored value, parsed to the declaration's type
                ["GetUnitValue"] = new NodeDefinition
                {
                    Type = "GetUnitValue",
                    InputPorts =
                    [
                        new DataPort { PortId = "unit",  Name = "Unit",  PortType = FieldType.Unit }
                    ],
                    OutputPorts =
                    [
                        new DataPort { PortId = "value", Name = "Value" }
                    ],
                    Evaluate = async (inputs, nodeValues, charId, session) =>
                    {
                        if (session == null)
                            throw new Exception("GetUnitValue requires a GameSession");

                        var templateId = GetIntNodeValue(nodeValues, "TemplateId");
                        var declId = GetIntNodeValue(nodeValues, "DeclarationId");

                        // the target unit id comes from the connected "Unit" data port
                        var targetUnitId = Convert.ToInt32(UnwrapJsonValue(
                            inputs.TryGetValue("Unit", out var raw) ? raw
                            : throw new Exception("GetUnitValue: missing 'Unit' input")));

                        var unit = GetUnit(session, targetUnitId);

                        if (unit.TemplateId != templateId)
                            throw new Exception(
                                $"GetUnitValue: unit {targetUnitId} belongs to template {unit.TemplateId}, " +
                                $"but node is configured for template {templateId}");

                        var decl = GetDeclaration(session, templateId, declId);
                        var value = unit.GetValueParsed(declId, decl.Type);

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?> { ["Value"] = value },
                            FlowOutputs = []
                        };
                    }
                },

                // ── SetValue ───────────────────────────────────────────────────────────
                // Sets a declaration value on the calling unit (identified by charId).
                // Throws if the incoming value is incompatible with the declaration's type.
                //
                // Node values (configured per node instance):
                //   "DeclarationId" – int  which declaration to write
                //
                // Inputs:  Set (flow), Value (any)  the new value to store
                // Outputs: Done (flow)  triggered after the value is written
                ["SetValue"] = new NodeDefinition
                {
                    Type = "SetValue",
                    InputPorts =
                    [
                        new FlowPort { PortId = "trigger", Name = "Set"   },
                        new DataPort  { PortId = "value",   Name = "Value", PortType = FieldType.Undefined }
                    ],
                    OutputPorts =
                    [
                        new FlowPort { PortId = "done", Name = "Done" }
                    ],
                    Execute = async (inputs, nodeValues, charId, session) =>
                    {
                        if (session == null)
                            throw new Exception("SetValue requires a GameSession");

                        var declId = GetIntNodeValue(nodeValues, "DeclarationId");
                        var unit = GetUnit(session, charId);

                        // Locate the declaration inside the unit's template to get the expected type
                        var decl = GetDeclaration(session, unit.TemplateId, declId);

                        // Unwrap the incoming value (handles JsonElement etc.)
                        var incoming = UnwrapJsonValue(
                            inputs.TryGetValue("Value", out var raw) ? raw : null);

                        // Validate and write; TrySetValue runs IsValidForType internally
                        if (!unit.TrySetValue(declId, incoming, decl.Type, out var error))
                            throw new Exception(
                                $"SetValue: value for declaration '{decl.Name}' is incompatible " +
                                $"with type {decl.Type} — {error}");

                        await Task.CompletedTask;

                        return new NodeDefinition.NodeExecutionResult
                        {
                            DataOutputs = new Dictionary<string, object?>(),
                            FlowOutputs = ["done"]
                        };
                    }
                }
            };

        // Seed NodeRegistry from the static dictionary.
        // Call once at application startup (e.g. in Program.cs before app.Run()).
        public static void RegisterDefaults()
        {
            foreach (var def in All.Values)
                NodeRegistry.Register(def);
        }

        // ── Session helpers ────────────────────────────────────────────────────────────

        private static Unit GetUnit(GameSession session, int unitId)
        {
            if (!session.Units.TryGetValue(unitId, out var unit))
                throw new Exception($"Unit {unitId} not found in session");
            return unit;
        }

        private static Decleration GetDeclaration(GameSession session, int templateId, int declId)
        {
            var template = session.Templates.Values.FirstOrDefault(t => t.Id == templateId)
                ?? throw new Exception($"Template {templateId} not found in session");

            var decl = template.Declerations.FirstOrDefault(d => d.Id == declId)
                ?? throw new Exception($"Declaration {declId} not found in template {templateId}");

            return decl;
        }

        // ── Value helpers ──────────────────────────────────────────────────────────────

        // Extract an int from a nodeValues entry, handling JsonElement transparently.
        private static int GetIntNodeValue(IDictionary<string, object?> nodeValues, string key)
        {
            if (!nodeValues.TryGetValue(key, out var raw))
                throw new Exception($"Node value '{key}' is missing");

            if (raw is JsonElement je)
            {
                if (je.TryGetInt32(out var i)) return i;
                throw new Exception($"Node value '{key}' is not a valid integer");
            }

            return Convert.ToInt32(raw);
        }

        private static object? UnwrapJsonValue(object? raw)
        {
            if (raw is null) return null;
            if (raw is JsonElement je)
            {
                return je.ValueKind switch
                {
                    JsonValueKind.String => je.GetString(),
                    JsonValueKind.Number => je.TryGetInt64(out var l) ? l
                                         : je.TryGetDouble(out var d) ? d
                                         : (object?)je.GetRawText(),
                    JsonValueKind.True => true,
                    JsonValueKind.False => false,
                    JsonValueKind.Null => null,
                    _ => je.GetRawText()
                };
            }

            return raw;
        }
    }
}
