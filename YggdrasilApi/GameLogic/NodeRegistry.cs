using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace YggdrasilApi.GameLogick;

// Registry for node definitions. Register your available node types here at startup.
public static class NodeRegistry
{
    static NodeRegistry()
    {
        // leave empty; user can register definitions at app startup
    }

    private static readonly Dictionary<string, NodeDefinition> _definitions = new Dictionary<string, NodeDefinition>();

    public static IReadOnlyDictionary<string, NodeDefinition> Definitions => new ReadOnlyDictionary<string, NodeDefinition>(_definitions);

    public static void Register(NodeDefinition def)
    {
        if (def == null) return;
        _definitions[def.Type] = def;
    }

    public static bool TryGet(string type, out NodeDefinition? def)
    {
        return _definitions.TryGetValue(type, out def);
    }
}
