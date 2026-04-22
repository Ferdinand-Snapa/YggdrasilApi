using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using YggdrasilApi.Models;

namespace YggdrasilApi.GameLogick;

public class RunGraph
{
    // Executes the provided graph. graphInput maps graph-level input port ids to values.
    // Returns a dictionary of produced values keyed by "{nodeId}:{portId}" for data outputs.
    public async Task<Dictionary<string, object?>> RunAsync(
        Graph graph,
        string charId,
        Dictionary<int, object?>? graphInput = null)
    {
        if (graph == null) throw new ArgumentNullException(nameof(graph));

        // local cache for produced data values: "nodeId:portId" -> value
        var localCache = new Dictionary<string, object?>();

        // helper to form cache key
        static string CacheKey(int nodeId, int portId) => nodeId + ":" + portId;

        // index nodes by id
        var nodesById = graph.Nodes.ToDictionary(n => n.Id);

        // determine start nodes: nodes with no incoming flow connections
        var nodesWithIncomingFlow = new HashSet<int>(
            graph.Connections
                .Where(c =>
                {
                    var toNode = nodesById.GetValueOrDefault(c.ToNodeId);
                    if (toNode == null) return false;
                    var inPort = toNode.Definition?.InputPorts.FirstOrDefault(p => p.PortId == c.ToPortId);
                    return inPort is FlowPort;
                })
                .Select(c => c.ToNodeId)
        );

        var queue = new Queue<Node>(graph.Nodes.Where(n => !nodesWithIncomingFlow.Contains(n.Id)));

        // main execution loop
        while (queue.Count > 0)
        {
            var node = queue.Dequeue();
            if (node.Definition == null)
                throw new Exception("Node definition is missing for node id: " + node.Id);

            // load persisted values into runtime dictionary
            node.LoadValues();

            // build inputs for this node (data inputs only)
            var inputs = new Dictionary<string, object?>();
            foreach (var inPort in node.Definition.InputPorts)
            {
                if (inPort is FlowPort) continue;

                // find connection feeding this input
                var conn = graph.Connections.FirstOrDefault(c => c.ToNodeId == node.Id && c.ToPortId == inPort.PortId);
                if (conn == null)
                {
                    // fallback to node.Values by name
                    if (node.Values.TryGetValue(inPort.Name, out var val)) inputs[inPort.Name] = val;
                    else throw new Exception($"Node id: {node.Id} has unconnected input port: {inPort.Name}");
                    continue;
                }

                // graph-level input: FromNodeId == 0
                if (conn.FromNodeId == 0)
                {
                    if (graphInput != null && graphInput.TryGetValue(conn.FromPortId, out var gv))
                    {
                        inputs[inPort.Name] = gv;
                    }
                    else throw new Exception($"Missing graph input for port id: {conn.FromPortId}");
                    continue;
                }

                // normal node-to-node connection
                var fromNode = nodesById.GetValueOrDefault(conn.FromNodeId)
                    ?? throw new Exception("Graph doesn't contain node id: " + conn.FromNodeId);

                var producedKey = CacheKey(fromNode.Id, conn.FromPortId);
                if (localCache.TryGetValue(producedKey, out var produced))
                {
                    inputs[inPort.Name] = produced;
                }
                else
                {
                    // need to evaluate producer node first (depth-first)
                    var producerResult = await EvaluateNodeAsync(graph, fromNode, graphInput, localCache);
                    // after evaluation, the value should be in cache
                    if (localCache.TryGetValue(producedKey, out var produced2))
                        inputs[inPort.Name] = produced2;
                    else
                        throw new Exception($"Missing value from {producedKey}");
                }
            }

            // execute or evaluate this node
            NodeDefinition.NodeExecutionResult? result = null;
            if (node.Definition.Evaluate != null)
            {
                result = node.Definition.Evaluate(inputs, node.Values);
            }
            else if (node.Definition.Execute != null)
            {
                result = await node.Definition.Execute(inputs, node.Values);
            }
            else
            {
                throw new Exception("Node definition has neither Evaluate nor Execute for node id: " + node.Id);
            }

            if (result == null) continue;

            // store data outputs into cache keyed by nodeId:portId
            foreach (var kv in result.DataOutputs)
            {
                // find the output port id by name
                var outPort = node.Definition.OutputPorts.FirstOrDefault(p => p.Name == kv.Key) as DataPort;
                if (outPort == null) continue; // or throw
                var key = CacheKey(node.Id, outPort.PortId);
                localCache[key] = kv.Value;
            }

            // enqueue downstream nodes for each flow output port id
            foreach (var flowPortId in result.FlowOutputs)
            {
                // Node recalls itself
                if (node.Definition.InputPorts.Where(p => p.PortId == flowPortId).Any())
                {
                    queue.Enqueue(node);
                    continue;
                }
                    
                var downstreamConn = graph.Connections.FirstOrDefault(c => c.FromNodeId == node.Id && c.FromPortId == flowPortId);

                // last node in graph or misconfigured connection
                if (downstreamConn == null)
                    continue;

                var nextNode = nodesById.GetValueOrDefault(downstreamConn.ToNodeId);
                if (nextNode != null)
                {
                    // avoid duplicate enqueueing
                    if (!queue.Contains(nextNode)) queue.Enqueue(nextNode);
                }

            }
        }

        return localCache;
    }

    // Evaluate a node (and recursively its dependencies) and populate localCache with produced data outputs.
    private async Task<NodeDefinition.NodeExecutionResult?> EvaluateNodeAsync(
        Graph graph,
        Node node,
        Dictionary<int, object?>? graphInput,
        Dictionary<string, object?> localCache)
    {
        if (node.Definition == null) throw new Exception("Node definition is null for node id: " + node.Id);
        node.LoadValues();

        // build inputs similar to RunAsync but without queuing
        var inputs = new Dictionary<string, object?>();
        foreach (var inPort in node.Definition.InputPorts)
        {
            if (inPort is FlowPort) continue;
            var conn = graph.Connections.FirstOrDefault(c => c.ToNodeId == node.Id && c.ToPortId == inPort.PortId);
            if (conn == null)
            {
                if (node.Values.TryGetValue(inPort.Name, out var val))
                    inputs[inPort.Name] = val;

                else
                    throw new Exception($"Node id: {node.Id} has unconnected input port: {inPort.Name}");
                continue;
            }

            if (conn.FromNodeId == 0)
            {
                if (graphInput != null && graphInput.TryGetValue(conn.FromPortId, out var gv)) inputs[inPort.Name] = gv;
                else throw new Exception($"Missing graph input for port id: {conn.FromPortId}");
                continue;
            }

            var fromNode = graph.Nodes.FirstOrDefault(n => n.Id == conn.FromNodeId) ?? throw new Exception("Graph doesn't contain node id: " + conn.FromNodeId);
            var producedKey = fromNode.Id + ":" + conn.FromPortId;
            if (localCache.TryGetValue(producedKey, out var produced)) inputs[inPort.Name] = produced;
            else
            {
                // recursively evaluate producer
                var prodResult = await EvaluateNodeAsync(graph, fromNode, graphInput, localCache);
                // store producer outputs into cache
                if (prodResult != null)
                {
                    foreach (var kv in prodResult.DataOutputs)
                    {
                        var outPort = fromNode.Definition!.OutputPorts.FirstOrDefault(p => p.Name == kv.Key) as DataPort;
                        if (outPort == null) continue;
                        localCache[fromNode.Id + ":" + outPort.PortId] = kv.Value;
                    }
                }

                if (localCache.TryGetValue(producedKey, out var produced2)) inputs[inPort.Name] = produced2;
                else throw new Exception($"Missing value from {producedKey}");
            }
        }

        NodeDefinition.NodeExecutionResult? result = null;

        if (node.Definition.Evaluate != null)
            result = node.Definition.Evaluate(inputs, node.Values);

        else if (node.Definition.Execute != null)
            throw new Exception("Execute Node read before it was ever called: " + node.Id);

        else
            throw new Exception("Node definition has neither Evaluate nor Execute for node id: " + node.Id);

        // populate cache for this node's outputs
        if (result != null)
        {
            foreach (var kv in result.DataOutputs)
            {
                var outPort = node.Definition.OutputPorts.FirstOrDefault(p => p.Name == kv.Key) as DataPort;
                if (outPort == null) continue;
                localCache[node.Id + ":" + outPort.PortId] = kv.Value;
            }
        }

        return result;
    }
}
